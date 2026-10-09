using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Roster.Application.Ports.Data;
using Roster.Domain.Games;

namespace Roster.Infrastructure.Data.Repositories;

internal sealed class GameRepository : IGameRepository
{
    private readonly RosterDbContext _context;

    public GameRepository(RosterDbContext context)
    {
        _context = context;
    }

    public async Task<Game?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Games.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public async Task<Game?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.Games
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Slug == slug, cancellationToken);
    }

    public async Task<Game?> GetByJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default)
    {
        return await _context.Games
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.JoinCode == joinCode, cancellationToken);
    }

    public async Task<List<Game>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Games
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Children are removed explicitly, in dependency order, so the result does not depend on cascade rules.
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        await _context.ScoreEntries.Where(x => x.GameId == id).ExecuteDeleteAsync(cancellationToken);
        await _context.Reports.Where(x => x.GameId == id).ExecuteDeleteAsync(cancellationToken);
        await _context.SponsorBonuses.Where(x => x.GameId == id).ExecuteDeleteAsync(cancellationToken);
        await _context.ConsentInvitations
            .Where(c => _context.Elements.Any(e => e.Id == c.ElementId && e.GameId == id))
            .ExecuteDeleteAsync(cancellationToken);
        await _context.Lineups
            .Where(l => _context.Participants.Any(p => p.Id == l.ParticipantId && p.GameId == id))
            .ExecuteDeleteAsync(cancellationToken);
        await _context.Participants.Where(x => x.GameId == id).ExecuteDeleteAsync(cancellationToken);
        await _context.Referees.Where(x => x.GameId == id).ExecuteDeleteAsync(cancellationToken);
        await _context.Rules.Where(x => x.GameId == id).ExecuteDeleteAsync(cancellationToken);
        await _context.Elements.Where(x => x.GameId == id).ExecuteDeleteAsync(cancellationToken);
        await _context.SourceBindings.Where(x => x.GameId == id).ExecuteDeleteAsync(cancellationToken);
        await _context.Games.Where(x => x.Id == id).ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        _context.ChangeTracker.Clear();
    }

    public void Add(Game game)
    {
        _context.Games.Add(game);
    }

    public void Update(Game game)
    {
        _context.Games.Update(game);
    }
}
