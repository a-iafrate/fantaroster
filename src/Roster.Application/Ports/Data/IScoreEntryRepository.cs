using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roster.Domain.Scores;

namespace Roster.Application.Ports.Data;

/// <summary>
/// Provides data access operations for ScoreEntry entities.
/// </summary>
public interface IScoreEntryRepository
{
    Task<List<ScoreEntry>> GetValidEntriesByGameIdAsync(Guid gameId, CancellationToken cancellationToken = default);
    Task<List<ScoreEntry>> GetRecentEntriesByGameIdAsync(Guid gameId, int count, CancellationToken cancellationToken = default);
    Task<bool> ExistsByIdempotencyKeyAsync(Guid gameId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<ScoreEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(ScoreEntry entry);
    void Update(ScoreEntry entry);
}
