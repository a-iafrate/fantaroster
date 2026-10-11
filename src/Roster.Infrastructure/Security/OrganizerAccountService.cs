using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Roster.Application.Ports.Data;
using Roster.Infrastructure.Data;

namespace Roster.Infrastructure.Security;

/// <summary>
/// The organizer's data rights: a copy of everything they hold in the service, and the deletion of the account together
/// with the games it owns. Token hashes and other secrets are never part of the export.
/// </summary>
public sealed class OrganizerAccountService(RosterDbContext db, UserManager<OrganizerUser> userManager, IGameRepository games)
{
    public async Task<OrganizerExport?> ExportAsync(string organizerId, TimeProvider time, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizerId);
        ArgumentNullException.ThrowIfNull(time);

        var user = await userManager.FindByIdAsync(organizerId);
        if (user is null)
        {
            return null;
        }

        var gameList = await db.Games.AsNoTracking()
            .Where(g => g.OrganizerId == organizerId)
            .OrderBy(g => g.CreatedAt)
            .ToListAsync(cancellationToken);

        var exported = new List<ExportedGame>();
        foreach (var game in gameList)
        {
            var elements = await db.Elements.AsNoTracking().Where(e => e.GameId == game.Id)
                .OrderBy(e => e.Name)
                .Select(e => new ExportedElement(e.Name, e.Subtitle, e.Group, e.ConsentStatus.ToString(), e.IsSelectable, e.IsHidden))
                .ToListAsync(cancellationToken);
            var rules = await db.Rules.AsNoTracking().Where(r => r.GameId == game.Id)
                .OrderBy(r => r.Order)
                .Select(r => new ExportedRule(r.Label, r.Points, r.Category, r.Target.ToString()))
                .ToListAsync(cancellationToken);
            var participants = await db.Participants.AsNoTracking().Where(p => p.GameId == game.Id)
                .OrderBy(p => p.JoinedAt)
                .Select(p => new ExportedParticipant(p.Nickname, p.JoinedAt))
                .ToListAsync(cancellationToken);
            var referees = await db.Referees.AsNoTracking().Where(r => r.GameId == game.Id)
                .OrderBy(r => r.DisplayName)
                .Select(r => r.DisplayName)
                .ToListAsync(cancellationToken);
            var scoreEntries = await (
                from s in db.ScoreEntries.AsNoTracking()
                where s.GameId == game.Id
                join r in db.Rules on s.RuleId equals r.Id
                orderby s.CreatedAt
                select new ExportedScoreEntry(r.Label, s.PointsSnapshot, s.Status.ToString(), s.CreatedAt, s.Note)).ToListAsync(cancellationToken);

            exported.Add(new ExportedGame(
                game.Name, game.DomainPackId, game.State.ToString(), game.CreatedAt, game.StartedAt, game.EndedAt, game.JoinCode,
                game.LineupSize, game.CaptainEnabled, game.CaptainMultiplier, elements, rules, participants, referees, scoreEntries));
        }

        return new OrganizerExport(time.GetUtcNow(), new ExportedAccount(user.Email, user.DisplayName, user.PreferredCulture, user.Plan), exported);
    }

    /// <summary>Deletes every game of the organizer and then the account. Returns false when the account does not exist.</summary>
    public async Task<bool> DeleteAccountAsync(string organizerId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizerId);

        var user = await userManager.FindByIdAsync(organizerId);
        if (user is null)
        {
            return false;
        }

        var gameIds = await db.Games.AsNoTracking().Where(g => g.OrganizerId == organizerId).Select(g => g.Id).ToListAsync(cancellationToken);
        foreach (var gameId in gameIds)
        {
            await games.DeleteAsync(gameId, cancellationToken);
        }

        var result = await userManager.DeleteAsync(user);
        return result.Succeeded;
    }
}

public sealed record OrganizerExport(DateTimeOffset ExportedAt, ExportedAccount Account, IReadOnlyList<ExportedGame> Games);

public sealed record ExportedAccount(string? Email, string DisplayName, string PreferredCulture, string Plan);

public sealed record ExportedGame(
    string Name,
    string DomainPackId,
    string State,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? EndedAt,
    string JoinCode,
    int LineupSize,
    bool CaptainEnabled,
    decimal CaptainMultiplier,
    IReadOnlyList<ExportedElement> Elements,
    IReadOnlyList<ExportedRule> Rules,
    IReadOnlyList<ExportedParticipant> Participants,
    IReadOnlyList<string> Referees,
    IReadOnlyList<ExportedScoreEntry> ScoreEntries);

public sealed record ExportedElement(string Name, string? Subtitle, string? Group, string ConsentStatus, bool IsSelectable, bool IsHidden);

public sealed record ExportedRule(string Label, int Points, string Category, string Target);

public sealed record ExportedParticipant(string Nickname, DateTimeOffset JoinedAt);

public sealed record ExportedScoreEntry(string Rule, int Points, string Status, DateTimeOffset CreatedAt, string? Note);
