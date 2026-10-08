using System.Runtime.CompilerServices;
using System.Text;

namespace Roster.Plugins.Csv;

/// <summary>
/// A lightweight, allocation-friendly CSV parser.
/// </summary>
internal sealed class SimpleCsvParser
{
    public static IReadOnlyList<string[]> Parse(string csvContent, char delimiter)
    {
        var result = new List<string[]>();
        var currentLine = new List<string>();
        var currentField = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < csvContent.Length; i++)
        {
            var c = csvContent[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < csvContent.Length && csvContent[i + 1] == '"')
                    {
                        currentField.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    currentField.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == delimiter)
                {
                    currentLine.Add(currentField.ToString());
                    currentField.Clear();
                }
                else if (c == '\r')
                {
                    if (i + 1 < csvContent.Length && csvContent[i + 1] == '\n')
                    {
                        i++;
                    }

                    currentLine.Add(currentField.ToString());
                    currentField.Clear();
                    result.Add(currentLine.ToArray());
                    currentLine.Clear();
                }
                else if (c == '\n')
                {
                    currentLine.Add(currentField.ToString());
                    currentField.Clear();
                    result.Add(currentLine.ToArray());
                    currentLine.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }
        }

        if (currentField.Length > 0 || currentLine.Count > 0)
        {
            currentLine.Add(currentField.ToString());
            result.Add(currentLine.ToArray());
        }

        return result;
    }

    public static char DetectDelimiter(string csvContent)
    {
        // Simple heuristic: find the first line, count commas and semicolons.
        var firstLineEnd = csvContent.IndexOf('\n');
        if (firstLineEnd < 0) firstLineEnd = csvContent.Length;

        var firstLine = csvContent.Substring(0, firstLineEnd);

        int commas = 0;
        int semicolons = 0;
        int tabs = 0;

        bool inQuotes = false;
        foreach (var c in firstLine)
        {
            if (c == '"') inQuotes = !inQuotes;
            if (!inQuotes)
            {
                if (c == ',') commas++;
                if (c == ';') semicolons++;
                if (c == '\t') tabs++;
            }
        }

        if (tabs > commas && tabs > semicolons) return '\t';
        if (semicolons > commas) return ';';
        return ',';
    }
}
