namespace Roster.Plugins.Abstractions;

/// <summary>
/// Defines a single configuration field required by a plugin.
/// </summary>
/// <param name="Key">The unique key of the configuration field.</param>
/// <param name="Label">The user-facing label for the field.</param>
/// <param name="Type">The type of the field (e.g., "text", "password", "file").</param>
/// <param name="IsRequired">Whether the field is required.</param>
/// <param name="Description">An optional description of the field.</param>
public sealed record ConfigField(
    string Key,
    string Label,
    string Type,
    bool IsRequired,
    string? Description = null);
