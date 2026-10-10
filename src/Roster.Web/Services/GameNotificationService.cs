using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Roster.Application.Ports.Notifications;
using Roster.Web.Hubs;

namespace Roster.Web.Services;

public sealed class GameNotificationService : IGameNotificationService
{
    private readonly IHubContext<GameHub> _hubContext;

    public GameNotificationService(IHubContext<GameHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyLeaderboardUpdatedAsync(Guid gameId, long version, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"game:{gameId}").SendAsync("LeaderboardUpdated", version, cancellationToken);
    }

    public async Task NotifyScoreEntryAddedAsync(Guid gameId, Guid scoreEntryId, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"game:{gameId}").SendAsync("ScoreEntryAdded", scoreEntryId, cancellationToken);
    }

    public async Task NotifyScoreEntryVoidedAsync(Guid gameId, Guid scoreEntryId, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"game:{gameId}").SendAsync("ScoreEntryVoided", scoreEntryId, cancellationToken);
    }

    public async Task NotifyGameStateChangedAsync(Guid gameId, string state, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"game:{gameId}").SendAsync("GameStateChanged", state, cancellationToken);
    }
}
