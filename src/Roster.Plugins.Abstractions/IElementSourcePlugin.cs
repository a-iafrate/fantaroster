namespace Roster.Plugins.Abstractions;

/// <summary>
/// The contract for an element source plugin, which imports elements from an external source.
/// </summary>
public interface IElementSourcePlugin
{
    /// <summary>
    /// Gets the unique identifier of the plugin (e.g. "sessionize", "csv").
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the display name of the plugin for the user interface.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the capabilities supported by the plugin.
    /// </summary>
    PluginCapabilities Capabilities { get; }

    /// <summary>
    /// Gets the configuration schema required to configure this plugin.
    /// </summary>
    /// <returns>The configuration schema.</returns>
    ConfigSchema GetConfigSchema();

    /// <summary>
    /// Validates the provided configuration.
    /// </summary>
    /// <param name="config">The configuration to validate.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous validation operation, yielding the validation result.</returns>
    Task<ValidationResult> ValidateConfigAsync(PluginConfig config, CancellationToken cancellationToken);

    /// <summary>
    /// Imports elements using the provided configuration.
    /// </summary>
    /// <param name="config">The configuration for the import operation.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous import operation, yielding the import result.</returns>
    Task<ImportResult> ImportAsync(PluginConfig config, CancellationToken cancellationToken);
}
