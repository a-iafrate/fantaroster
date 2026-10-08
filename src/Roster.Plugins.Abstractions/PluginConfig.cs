namespace Roster.Plugins.Abstractions;

/// <summary>
/// Represents the configuration values provided by the user for a plugin.
/// </summary>
/// <param name="Values">The dictionary of configuration values, mapped by the schema keys.</param>
public sealed record PluginConfig(
    IReadOnlyDictionary<string, string> Values);
