using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Roster.Application.Ports.Data;
using Roster.Domain.Referees;

namespace Roster.Infrastructure.Data.Repositories;

internal sealed class RefereeRepository : IRefereeRepository
{
    private readonly RosterDbContext _context;

    public RefereeRepository(RosterDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Referee>> GetByGameIdAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        return await _context.Referees
            .Where(r => r.GameId == gameId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Referee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Referees.FindAsync(new object[] { id }, cancellationToken);
    }

    public void Add(Referee referee)
    {
        _context.Referees.Add(referee);
    }

    public void Delete(Referee referee)
    {
        _context.Referees.Remove(referee);
    }
}
