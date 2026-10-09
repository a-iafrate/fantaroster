using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Roster.Application.Ports.Data;
using Roster.Domain.Scores;

namespace Roster.Infrastructure.Data.Repositories;

internal sealed class ScoreEntryRepository : IScoreEntryRepository
{
    private readonly RosterDbContext _context;

    public ScoreEntryRepository(RosterDbContext context)
    {
        _context = context;
    }

    public async Task<List<ScoreEntry>> GetValidEntriesByGameIdAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        return await _context.ScoreEntries
            .AsNoTracking()
            .Where(s => s.GameId == gameId && s.Status == ScoreStatus.Valid)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByIdempotencyKeyAsync(Guid gameId, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _context.ScoreEntries
            .AsNoTracking()
            .AnyAsync(s => s.GameId == gameId && s.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task<List<ScoreEntry>> GetRecentEntriesByGameIdAsync(Guid gameId, int count, CancellationToken cancellationToken = default)
    {
        return await _context.ScoreEntries
            .AsNoTracking()
            .Where(s => s.GameId == gameId)
            .OrderByDescending(s => s.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public void Add(ScoreEntry entry)
    {
        _context.ScoreEntries.Add(entry);
    }

    public void Update(ScoreEntry entry)
    {
        _context.ScoreEntries.Update(entry);
    }
}
