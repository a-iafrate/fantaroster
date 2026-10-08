namespace Roster.Plugins.Abstractions;

/// <summary>
/// Represents the result of an import operation from a source plugin.
/// </summary>
/// <param name="Elements">The list of elements successfully imported or parsed.</param>
/// <param name="Warnings">A list of warnings encountered during the import process (e.g. invalid lines in a CSV).</param>
public sealed record ImportResult(
    IReadOnlyList<ImportedElement> Elements,
    IReadOnlyList<string> Warnings);
