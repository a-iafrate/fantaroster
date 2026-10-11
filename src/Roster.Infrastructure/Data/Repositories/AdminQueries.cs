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

    public async Task<AdminStats> GetStatsAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var organizers = await context.Users.CountAsync(cancellationToken);
        var gamesByState = await context.Games
            .GroupBy(g => g.State)
            .Select(g => new { State = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var participants = await context.Participants.CountAsync(cancellationToken);
        var weekAgo = now.AddDays(-7);
        var participantsLastWeek = await context.Participants.CountAsync(p => p.JoinedAt >= weekAgo, cancellationToken);
        var valid = await context.ScoreEntries.CountAsync(s => s.Status == ScoreStatus.Valid, cancellationToken);
        var voided = await context.ScoreEntries.CountAsync(s => s.Status == ScoreStatus.Voided, cancellationToken);
        var pending = await context.CommunityRequests.CountAsync(r => r.Status == CommunityRequestStatus.Pending, cancellationToken);
        var approved = await context.CommunityRequests
            .Where(r => r.Status == CommunityRequestStatus.Approved)
            .Select(r => r.OrganizerId)
            .Distinct()
            .CountAsync(cancellationToken);

        // Joins per day: grouped in memory, the window is small.
        var since = now.AddDays(-(JoinDays - 1));
        var firstDay = new DateTimeOffset(since.UtcDateTime.Date, TimeSpan.Zero);
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

        var top = (await GetGamesAsync(cancellationToken)).OrderByDescending(g => g.Participants).Take(TopGamesCount).ToList();

        return new AdminStats(
            organizers,
            gamesByState.Sum(g => g.Count),
            gamesByState.ToDictionary(g => g.State, g => g.Count),
            participants,
            participantsLastWeek,
            valid,
            voided,
            pending,
            approved,
            joins,
            top);
    }

    public async Task<List<AdminGameRow>> GetGamesAsync(CancellationToken cancellationToken = default)
    {
        var rows = await (
            from g in context.Games.AsNoTracking()
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
                Participants = context.Participants.Count(p => p.GameId == g.Id),
            }).ToListAsync(cancellationToken);

        var communityOrganizers = (await context.CommunityRequests
            .Where(r => r.Status == CommunityRequestStatus.Approved)
            .Select(r => r.OrganizerId)
            .ToListAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal);

        return rows
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new AdminGameRow(
                r.Id,
                r.Name,
                r.OrganizerEmail,
                r.OrganizerId is not null && communityOrganizers.Contains(r.OrganizerId),
                r.State,
                r.Participants,
                r.CreatedAt,
                r.ParticipantLimitOverride,
                r.LimitLabel))
            .ToList();
    }
}
