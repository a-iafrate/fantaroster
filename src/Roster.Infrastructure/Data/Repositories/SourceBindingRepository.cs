using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Roster.Application.Ports.Data;
using Roster.Domain.Sources;

namespace Roster.Infrastructure.Data.Repositories;

internal sealed class SourceBindingRepository : ISourceBindingRepository
{
    private readonly RosterDbContext _dbContext;

    public SourceBindingRepository(RosterDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<SourceBinding?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SourceBindings.FirstOrDefaultAsync(sb => sb.Id == id, cancellationToken);
    }

    public async Task<List<SourceBinding>> GetByGameIdAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SourceBindings.Where(sb => sb.GameId == gameId).ToListAsync(cancellationToken);
    }

    public void Add(SourceBinding binding)
    {
        _dbContext.SourceBindings.Add(binding);
    }

    public void Update(SourceBinding binding)
    {
        _dbContext.SourceBindings.Update(binding);
    }
}
