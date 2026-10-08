namespace Roster.Plugins.Abstractions;

/// <summary>
/// Represents an element imported from an external source plugin.
/// </summary>
/// <param name="ExternalId">The stable external identifier of the element.</param>
/// <param name="Name">The primary name of the element.</param>
/// <param name="Subtitle">An optional subtitle (e.g. session title, team city, song title).</param>
/// <param name="ImageUrl">An optional image URL (e.g. a photo, a team crest, an artwork).</param>
/// <param name="Group">An optional group (e.g. room, pitch, group, category).</param>
/// <param name="Metadata">Additional metadata attached to the element.</param>
public sealed record ImportedElement(
    string ExternalId,
    string Name,
    string? Subtitle,
    string? ImageUrl,
    string? Group,
    IReadOnlyDictionary<string, string> Metadata);
