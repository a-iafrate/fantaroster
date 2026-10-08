using Roster.Application.Ports;
using Roster.Plugins.Abstractions;

namespace Roster.Infrastructure.Plugins;

/// <summary>
/// Infrastructure implementation of the plugin registry.
/// </summary>
public sealed class PluginRegistry : IPluginRegistry
{
    private readonly Dictionary<string, IElementSourcePlugin> _plugins;

    public PluginRegistry(IEnumerable<IElementSourcePlugin> plugins)
    {
        ArgumentNullException.ThrowIfNull(plugins);
        _plugins = plugins.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IEnumerable<IElementSourcePlugin> GetAllPlugins() => _plugins.Values;

    public IElementSourcePlugin? GetPlugin(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return _plugins.TryGetValue(id, out var plugin) ? plugin : null;
    }
}
