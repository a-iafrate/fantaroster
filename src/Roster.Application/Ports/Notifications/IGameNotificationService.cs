using System;
using System.Threading;
using System.Threading.Tasks;

namespace Roster.Application.Ports.Notifications;

public interface IGameNotificationService
{
    Task NotifyLeaderboardUpdatedAsync(Guid gameId, long version, CancellationToken cancellationToken = default);
    Task NotifyScoreEntryAddedAsync(Guid gameId, Guid scoreEntryId, CancellationToken cancellationToken = default);
    Task NotifyScoreEntryVoidedAsync(Guid gameId, Guid scoreEntryId, CancellationToken cancellationToken = default);
    Task NotifyGameStateChangedAsync(Guid gameId, string state, CancellationToken cancellationToken = default);
}
