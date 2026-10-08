using System;

namespace Roster.Domain.Sources;

public sealed class SourceBinding
{
    public Guid Id { get; private set; }
    public Guid GameId { get; private set; }
    public string PluginId { get; private set; }
    public string ConfigurationJson { get; private set; }
    public string? SecretReference { get; private set; }
    public DateTimeOffset? LastSyncTime { get; private set; }
    public string? LastSyncResult { get; private set; }

    private SourceBinding()
    {
        PluginId = string.Empty;
        ConfigurationJson = "{}";
    }

    public SourceBinding(Guid id, Guid gameId, string pluginId, string configurationJson, string? secretReference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);

        Id = id;
        GameId = gameId;
        PluginId = pluginId;
        ConfigurationJson = configurationJson;
        SecretReference = secretReference;
    }

    public void RecordSync(DateTimeOffset syncTime, string result)
    {
        LastSyncTime = syncTime;
        LastSyncResult = result;
    }
}
