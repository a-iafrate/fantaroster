using System;
using System.Collections.Generic;
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

    public void Add(Game game)
    {
        _context.Games.Add(game);
    }

    public void Update(Game game)
    {
        _context.Games.Update(game);
    }
}
