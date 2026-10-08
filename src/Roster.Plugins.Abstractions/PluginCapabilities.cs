namespace Roster.Plugins.Abstractions;

/// <summary>
/// Defines the capabilities of an element source plugin.
/// </summary>
[Flags]
public enum PluginCapabilities
{
    /// <summary>No capabilities.</summary>
    None = 0,

    /// <summary>Can import elements from a source.</summary>
    Import = 1,

    /// <summary>Can resynchronize existing elements with the source.</summary>
    Resync = 2,

    /// <summary>Can provide a preview of the elements before importing.</summary>
    Preview = 4
}
