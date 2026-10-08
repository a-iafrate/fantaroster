using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Roster.Application.Ports.Data;
using Roster.Domain.Elements;

namespace Roster.Infrastructure.Data.Repositories;

internal sealed class ElementRepository : IElementRepository
{
    private readonly RosterDbContext _context;

    public ElementRepository(RosterDbContext context)
    {
        _context = context;
    }

    public async Task<Element?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Elements.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<List<Element>> GetByGameIdAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        return await _context.Elements
            .AsNoTracking()
            .Where(e => e.GameId == gameId)
            .ToListAsync(cancellationToken);
    }

    public void Add(Element element)
    {
        _context.Elements.Add(element);
    }

    public void Update(Element element)
    {
        _context.Elements.Update(element);
    }
}
