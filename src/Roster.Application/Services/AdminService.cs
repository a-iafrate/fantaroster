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

    /// <summary>One page of games for the administration list: search, filters, order and paging are done by the database.</summary>
    public async Task<AdminGamePage> SearchGamesAsync(AdminGameQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var pageSize = Math.Clamp(query.PageSize, 1, AdminGameQuery.MaxPageSize);
        var normalized = query with { Page = Math.Max(1, query.Page), PageSize = pageSize, Search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim() };
        var (rows, total) = await queries.SearchGamesAsync(normalized, cancellationToken);
        return new AdminGamePage(rows.Select(ToAdminGame).ToList(), total, normalized.Page, pageSize);
    }

    private AdminGame ToAdminGame(AdminGameRow r) => new(
        r.Id,
        r.Name,
        r.OrganizerEmail,
        r.State,
        r.Participants,
        r.CreatedAt,
        r.LimitOverride ?? (r.OrganizerIsCommunity ? Math.Max(limits.FreeParticipantLimit, limits.CommunityParticipantLimit) : limits.FreeParticipantLimit),
        r.LimitOverride,
        r.LimitLabel);

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
