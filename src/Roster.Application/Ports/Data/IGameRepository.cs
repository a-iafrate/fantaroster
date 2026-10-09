using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roster.Domain.Games;

namespace Roster.Application.Ports.Data;

/// <summary>
/// Provides data access operations for Game entities.
/// </summary>
public interface IGameRepository
{
    Task<Game?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Game?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Game?> GetByJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default);
    Task<List<Game>> GetAllAsync(CancellationToken cancellationToken = default);
    /// <summary>Permanently deletes a game together with everything that belongs to it.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(Game game);
    void Update(Game game);
}
