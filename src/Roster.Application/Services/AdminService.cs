using Roster.Application.Admin;
using Roster.Application.Plans;
using Roster.Application.Ports.Data;

namespace Roster.Application.Services;

/// <summary>What the internal administration needs: statistics, the list of games and the per-game participant limit.</summary>
public sealed class AdminService(
    IAdminQueries queries,
    IGameRepository games,
    IUnitOfWork unitOfWork,
    PlanLimits limits,
    TimeProvider timeProvider)
{
    public Task<AdminStats> GetStatsAsync(CancellationToken cancellationToken = default) =>
        queries.GetStatsAsync(timeProvider.GetUtcNow(), cancellationToken);

    public async Task<List<AdminGame>> GetGamesAsync(CancellationToken cancellationToken = default)
    {
        var rows = await queries.GetGamesAsync(cancellationToken);
        return rows
            .Select(r => new AdminGame(
                r.Id,
                r.Name,
                r.OrganizerEmail,
                r.State,
                r.Participants,
                r.CreatedAt,
                r.LimitOverride ?? (r.OrganizerIsCommunity ? Math.Max(limits.FreeParticipantLimit, limits.CommunityParticipantLimit) : limits.FreeParticipantLimit),
                r.LimitOverride,
                r.LimitLabel))
            .ToList();
    }

    /// <summary>Sets (or clears, with <c>null</c>) the participant limit of one game. Returns <c>false</c> when the game does not exist.</summary>
    public async Task<bool> SetGameLimitAsync(Guid gameId, int? limit, string? label, CancellationToken cancellationToken = default)
    {
        var game = await games.GetByIdAsync(gameId, cancellationToken);
        if (game is null)
        {
            return false;
        }

        game.SetParticipantLimitOverride(limit, label);
        games.Update(game);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
