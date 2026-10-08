using System.Net.Http.Json;
using System.Text.Json;
using Roster.Plugins.Abstractions;

namespace Roster.Plugins.Sessionize;

/// <summary>
/// A plugin to import speakers (and optionally sessions) from a Sessionize API endpoint.
/// </summary>
public sealed class SessionizeElementSourcePlugin : IElementSourcePlugin
{
    private readonly HttpClient _httpClient;

    public SessionizeElementSourcePlugin(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public string Id => "sessionize";
    public string DisplayName => "Sessionize Import";
    public PluginCapabilities Capabilities => PluginCapabilities.Import | PluginCapabilities.Resync | PluginCapabilities.Preview;

    public ConfigSchema GetConfigSchema()
    {
        return new ConfigSchema([
            new ConfigField("EndpointId", "Endpoint ID", "text", true, "The unique ID of the Sessionize API endpoint (e.g. from https://sessionize.com/api/v2/{endpointId}/view/All)."),
            new ConfigField("ImportSessions", "Import Sessions too", "checkbox", false, "If checked, sessions will be imported as elements as well as speakers.")
        ]);
    }

    public Task<ValidationResult> ValidateConfigAsync(PluginConfig config, CancellationToken cancellationToken)
    {
        if (!config.Values.TryGetValue("EndpointId", out var endpointId) || string.IsNullOrWhiteSpace(endpointId))
        {
            return Task.FromResult(ValidationResult.Failure("Endpoint ID is required."));
        }

        if (!endpointId.All(c => char.IsLetterOrDigit(c) || c == '-'))
        {
            return Task.FromResult(ValidationResult.Failure("Endpoint ID format is invalid. It should be an alphanumeric string."));
        }

        return Task.FromResult(ValidationResult.Success());
    }

    public async Task<ImportResult> ImportAsync(PluginConfig config, CancellationToken cancellationToken)
    {
        if (!config.Values.TryGetValue("EndpointId", out var endpointId) || string.IsNullOrWhiteSpace(endpointId))
        {
            return new ImportResult(Array.Empty<ImportedElement>(), ["Endpoint ID is not configured."]);
        }

        bool importSessions = config.Values.TryGetValue("ImportSessions", out var impSessionsStr) && bool.TryParse(impSessionsStr, out var parsed) && parsed;

        var url = $"https://sessionize.com/api/v2/{endpointId}/view/All";

        SessionizeAllResponse? response;
        try
        {
            var httpResponse = await _httpClient.GetAsync(url, cancellationToken);
            if (!httpResponse.IsSuccessStatusCode)
            {
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new ImportResult(Array.Empty<ImportedElement>(), ["The Sessionize endpoint was not found or is not enabled."]);
                }
                return new ImportResult(Array.Empty<ImportedElement>(), [$"Sessionize API returned an error: {httpResponse.StatusCode}"]);
            }

            response = await httpResponse.Content.ReadFromJsonAsync<SessionizeAllResponse>(cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return new ImportResult(Array.Empty<ImportedElement>(), [$"Failed to connect to Sessionize: {ex.Message}"]);
        }
        catch (JsonException ex)
        {
            return new ImportResult(Array.Empty<ImportedElement>(), [$"Failed to parse Sessionize response: {ex.Message}"]);
        }

        if (response == null)
        {
            return new ImportResult(Array.Empty<ImportedElement>(), ["Sessionize API returned an empty response."]);
        }

        var elements = new List<ImportedElement>();
        var warnings = new List<string>();

        if (response.Speakers != null)
        {
            foreach (var speaker in response.Speakers)
            {
                var name = speaker.FullName ?? $"{speaker.FirstName} {speaker.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    warnings.Add($"Speaker {speaker.Id} has no name, skipped.");
                    continue;
                }

                elements.Add(new ImportedElement(
                    ExternalId: $"spk-{speaker.Id}",
                    Name: name,
                    Subtitle: speaker.TagLine,
                    ImageUrl: speaker.ProfilePicture,
                    Group: "Speaker",
                    Metadata: new Dictionary<string, string>()
                ));
            }
        }

        if (importSessions && response.Sessions != null)
        {
            foreach (var session in response.Sessions)
            {
                var title = session.Title;
                if (string.IsNullOrWhiteSpace(title))
                {
                    warnings.Add($"Session {session.Id} has no title, skipped.");
                    continue;
                }

                elements.Add(new ImportedElement(
                    ExternalId: $"sess-{session.Id}",
                    Name: title,
                    Subtitle: null,
                    ImageUrl: null,
                    Group: "Session",
                    Metadata: new Dictionary<string, string>()
                ));
            }
        }

        return new ImportResult(elements, warnings);
    }
}
