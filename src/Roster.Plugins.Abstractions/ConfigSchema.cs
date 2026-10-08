namespace Roster.Plugins.Abstractions;

/// <summary>
/// Defines the configuration schema for a plugin, specifying what fields are required.
/// </summary>
/// <param name="Fields">The list of configuration fields.</param>
public sealed record ConfigSchema(
    IReadOnlyList<ConfigField> Fields);
