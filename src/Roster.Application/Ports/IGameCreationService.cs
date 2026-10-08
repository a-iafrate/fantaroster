using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Roster.Application.Ports;

public sealed record CreateGameCommand(
    string Name,
    string DomainPackId,
    string Brand,
    string Culture,
    int LineupSize,
    bool CaptainEnabled,
    decimal CaptainMultiplier,
    string? PluginId,
    Dictionary<string, string>? PluginConfig
);

public interface IGameCreationService
{
    Task<Guid> CreateGameAsync(CreateGameCommand command, CancellationToken cancellationToken);
}
