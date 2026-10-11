using Microsoft.EntityFrameworkCore;
using Roster.Application.Admin;
using Roster.Application.Ports.Data;
using Roster.Domain.Games;
using Roster.Domain.Organizers;
using Roster.Domain.Scores;

namespace Roster.Infrastructure.Data.Repositories;

internal sealed class AdminQueries(RosterDbContext context) : IAdminQueries
{
    private const int TopGamesCount = 10;
    private const int JoinDays = 14;

    /// <summary>
    /// A handful of small aggregate queries, none of them reading whole tables: the database may be far away, so every
    /// round trip counts.
    /// </summary>
    public async Task<AdminStats> GetStatsAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var organizers = await context.Users.CountAsync(cancellationToken);

        var gamesByState = await context.Games
            .GroupBy(g => g.State)
            .Select(g => new { State = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var weekAgo = now.AddDays(-7);
        var participantTotals = await context.Participants
            .GroupBy(_ => 1)
            .Select(g => new { Total = g.Count(), LastWeek = g.Count(p => p.JoinedAt >= weekAgo) })
            .FirstOrDefaultAsync(cancellationToken);

        var scores = await context.ScoreEntries
            .GroupBy(s => s.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        // The community requests are few: read them once for the counts and for the owners of the top games.
        var requests = await context.CommunityRequests
            .Select(r => new { r.OrganizerId, r.Status })
            .ToListAsync(cancellationToken);
        var communityOrganizers = requests.Where(r => r.Status == CommunityRequestStatus.Approved).Select(r => r.OrganizerId).ToHashSet(StringComparer.Ordinal);

        var firstDay = new DateTimeOffset(now.UtcDateTime.AddDays(-(JoinDays - 1)).Date, TimeSpan.Zero);
        var joined = await context.Participants
            .Where(p => p.JoinedAt >= firstDay)
            .Select(p => p.JoinedAt)
            .ToListAsync(cancellationToken);
        var perDay = joined.GroupBy(j => DateOnly.FromDateTime(j.UtcDateTime)).ToDictionary(g => g.Key, g => g.Count());
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var joins = Enumerable.Range(0, JoinDays)
            .Select(i => today.AddDays(-(JoinDays - 1 - i)))
            .Select(day => new DailyCount(day, perDay.GetValueOrDefault(day)))
            .ToList();

        var topCounts = await context.Participants
            .GroupBy(p => p.GameId)
            .Select(g => new { GameId = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(TopGamesCount)
            .ToListAsync(cancellationToken);
        var topIds = topCounts.Select(t => t.GameId).ToList();
        var topGames = topIds.Count == 0 ? [] : await ReadGamesAsync(topIds, communityOrganizers, topCounts.ToDictionary(t => t.GameId, t => t.Count), cancellationToken);

        return new AdminStats(
            organizers,
            gamesByState.Sum(g => g.Count),
            gamesByState.ToDictionary(g => g.State, g => g.Count),
            participantTotals?.Total ?? 0,
            participantTotals?.LastWeek ?? 0,
            scores.Where(s => s.Status == ScoreStatus.Valid).Sum(s => s.Count),
            scores.Where(s => s.Status == ScoreStatus.Voided).Sum(s => s.Count),
            requests.Count(r => r.Status == CommunityRequestStatus.Pending),
            communityOrganizers.Count,
            joins,
            topGames.OrderByDescending(g => g.Participants).ToList());
    }

    public async Task<List<AdminGameRow>> GetGamesAsync(CancellationToken cancellationToken = default)
    {
        var communityOrganizers = (await context.CommunityRequests
            .Where(r => r.Status == CommunityRequestStatus.Approved)
            .Select(r => r.OrganizerId)
            .ToListAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal);

        var counts = await context.Participants
            .GroupBy(p => p.GameId)
            .Select(g => new { GameId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.GameId, g => g.Count, cancellationToken);

        return (await ReadGamesAsync(null, communityOrganizers, counts, cancellationToken)).OrderByDescending(g => g.CreatedAt).ToList();
    }

    /// <summary>The games (all, or the given ones) with the email of their organizer.</summary>
    private async Task<List<AdminGameRow>> ReadGamesAsync(
        List<Guid>? ids,
        HashSet<string> communityOrganizers,
        Dictionary<Guid, int> participantCounts,
        CancellationToken cancellationToken)
    {
        var games = context.Games.AsNoTracking();
        if (ids is not null)
        {
            games = games.Where(g => ids.Contains(g.Id));
        }

        var rows = await (
            from g in games
            join u in context.Users on g.OrganizerId equals u.Id into owners
            from owner in owners.DefaultIfEmpty()
            select new
            {
                g.Id,
                g.Name,
                OrganizerEmail = owner == null ? null : owner.Email,
                g.OrganizerId,
                g.State,
                g.CreatedAt,
                g.ParticipantLimitOverride,
                g.LimitLabel,
            }).ToListAsync(cancellationToken);

        return rows
            .Select(r => new AdminGameRow(
                r.Id,
                r.Name,
                r.OrganizerEmail,
                r.OrganizerId is not null && communityOrganizers.Contains(r.OrganizerId),
                r.State,
                participantCounts.GetValueOrDefault(r.Id),
                r.CreatedAt,
                r.ParticipantLimitOverride,
                r.LimitLabel))
            .ToList();
    }
}
