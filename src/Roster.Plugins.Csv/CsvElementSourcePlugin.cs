using Roster.Plugins.Abstractions;

namespace Roster.Plugins.Csv;

/// <summary>
/// A plugin to import elements from a CSV payload.
/// </summary>
public sealed class CsvElementSourcePlugin : IElementSourcePlugin
{
    private const int MaxCsvLength = 5 * 1024 * 1024; // 5 MB

    public string Id => "csv";
    public string DisplayName => "CSV Import";
    public PluginCapabilities Capabilities => PluginCapabilities.Import | PluginCapabilities.Preview;

    public ConfigSchema GetConfigSchema()
    {
        return new ConfigSchema(new[]
        {
            new ConfigField("CsvContent", "CSV Content", "textarea", true, "Paste the CSV content here. Maximum size 5 MB."),
            new ConfigField("Delimiter", "Delimiter", "text", false, "Leave empty to auto-detect (comma, semicolon or tab)."),
            new ConfigField("HasHeader", "Has Header Row", "checkbox", false, "Check if the first row contains column names. Default is true."),
            new ConfigField("ColumnExternalId", "External ID Column", "text", false, "Name or 0-based index of the External ID column."),
            new ConfigField("ColumnName", "Name Column", "text", true, "Name or 0-based index of the Name column."),
            new ConfigField("ColumnSubtitle", "Subtitle Column", "text", false, "Name or 0-based index of the Subtitle column."),
            new ConfigField("ColumnImageUrl", "Image URL Column", "text", false, "Name or 0-based index of the Image URL column."),
            new ConfigField("ColumnGroup", "Group Column", "text", false, "Name or 0-based index of the Group column.")
        });
    }

    public Task<ValidationResult> ValidateConfigAsync(PluginConfig config, CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        if (!config.Values.TryGetValue("CsvContent", out var content) || string.IsNullOrWhiteSpace(content))
        {
            errors.Add("CSV Content is required.");
        }
        else if (content.Length > MaxCsvLength)
        {
            errors.Add($"CSV Content exceeds the maximum allowed size of {MaxCsvLength / 1024 / 1024} MB.");
        }

        if (!config.Values.TryGetValue("ColumnName", out var nameCol) || string.IsNullOrWhiteSpace(nameCol))
        {
            errors.Add("Name Column mapping is required.");
        }

        return Task.FromResult(errors.Count > 0 ? ValidationResult.Failure(errors) : ValidationResult.Success());
    }

    public Task<ImportResult> ImportAsync(PluginConfig config, CancellationToken cancellationToken)
    {
        if (!config.Values.TryGetValue("CsvContent", out var content) || string.IsNullOrWhiteSpace(content))
        {
            return Task.FromResult(new ImportResult(Array.Empty<ImportedElement>(), ["No CSV content provided."]));
        }

        var delimiterStr = config.Values.GetValueOrDefault("Delimiter");
        char delimiter = string.IsNullOrEmpty(delimiterStr) ? SimpleCsvParser.DetectDelimiter(content) : delimiterStr[0];

        bool hasHeader = true;
        if (config.Values.TryGetValue("HasHeader", out var hasHeaderStr) && bool.TryParse(hasHeaderStr, out var parsedHasHeader))
        {
            hasHeader = parsedHasHeader;
        }

        var rows = SimpleCsvParser.Parse(content, delimiter);
        if (rows.Count == 0)
        {
            return Task.FromResult(new ImportResult(Array.Empty<ImportedElement>(), ["CSV is empty."]));
        }

        var elements = new List<ImportedElement>();
        var warnings = new List<string>();

        var headerRow = hasHeader ? rows[0] : null;
        var dataRows = hasHeader ? rows.Skip(1).ToList() : rows;

        var colName = config.Values.GetValueOrDefault("ColumnName") ?? "0";
        var colExternalId = config.Values.GetValueOrDefault("ColumnExternalId");
        var colSubtitle = config.Values.GetValueOrDefault("ColumnSubtitle");
        var colImageUrl = config.Values.GetValueOrDefault("ColumnImageUrl");
        var colGroup = config.Values.GetValueOrDefault("ColumnGroup");

        int nameIdx = ResolveColumnIndex(colName, headerRow);
        int extIdIdx = ResolveColumnIndex(colExternalId, headerRow);
        int subtitleIdx = ResolveColumnIndex(colSubtitle, headerRow);
        int imageUrlIdx = ResolveColumnIndex(colImageUrl, headerRow);
        int groupIdx = ResolveColumnIndex(colGroup, headerRow);

        if (nameIdx < 0)
        {
            warnings.Add($"Could not resolve Name column '{colName}'. Import aborted.");
            return Task.FromResult(new ImportResult(Array.Empty<ImportedElement>(), warnings));
        }

        int rowNumber = hasHeader ? 2 : 1;
        foreach (var row in dataRows)
        {
            if (row.Length == 0 || (row.Length == 1 && string.IsNullOrWhiteSpace(row[0])))
            {
                rowNumber++;
                continue;
            }

            if (nameIdx >= row.Length || string.IsNullOrWhiteSpace(row[nameIdx]))
            {
                warnings.Add($"Row {rowNumber}: Missing or empty value for Name column. Row skipped.");
                rowNumber++;
                continue;
            }

            var name = row[nameIdx].Trim();

            // Generate a fallback external ID if none is provided or mapped.
            // A stable ID is required for resync. If the CSV doesn't have one, we use a hash of the name.
            string extId = string.Empty;
            if (extIdIdx >= 0 && extIdIdx < row.Length && !string.IsNullOrWhiteSpace(row[extIdIdx]))
            {
                extId = row[extIdIdx].Trim();
            }
            else
            {
                extId = "csv-auto-" + Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(name))).Substring(0, 12).Replace("/", "_").Replace("+", "-");
            }

            string? subtitle = subtitleIdx >= 0 && subtitleIdx < row.Length && !string.IsNullOrWhiteSpace(row[subtitleIdx]) ? row[subtitleIdx].Trim() : null;
            string? imageUrl = imageUrlIdx >= 0 && imageUrlIdx < row.Length && !string.IsNullOrWhiteSpace(row[imageUrlIdx]) ? row[imageUrlIdx].Trim() : null;
            string? group = groupIdx >= 0 && groupIdx < row.Length && !string.IsNullOrWhiteSpace(row[groupIdx]) ? row[groupIdx].Trim() : null;

            elements.Add(new ImportedElement(
                ExternalId: extId,
                Name: name,
                Subtitle: subtitle,
                ImageUrl: imageUrl,
                Group: group,
                Metadata: new Dictionary<string, string>() // Can map remaining columns here later if needed
            ));

            rowNumber++;
        }

        return Task.FromResult(new ImportResult(elements, warnings));
    }

    private static int ResolveColumnIndex(string? columnNameOrIndex, string[]? headerRow)
    {
        if (string.IsNullOrWhiteSpace(columnNameOrIndex))
            return -1;

        if (int.TryParse(columnNameOrIndex, out var idx))
            return idx;

        if (headerRow != null)
        {
            for (int i = 0; i < headerRow.Length; i++)
            {
                if (string.Equals(headerRow[i].Trim(), columnNameOrIndex.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
        }

        return -1;
    }
}
