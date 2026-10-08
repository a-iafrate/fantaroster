using Roster.Plugins.Abstractions;

namespace Roster.Application.Ports;

/// <summary>
/// Provides access to registered element source plugins.
/// </summary>
public interface IPluginRegistry
{
    /// <summary>
    /// Gets all registered element source plugins.
    /// </summary>
    /// <returns>A collection of registered plugins.</returns>
    IEnumerable<IElementSourcePlugin> GetAllPlugins();

    /// <summary>
    /// Gets a specific plugin by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the plugin.</param>
    /// <returns>The plugin, or null if not found.</returns>
    IElementSourcePlugin? GetPlugin(string id);
}
