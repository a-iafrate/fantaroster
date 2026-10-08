using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Roster.Application.Ports.Data;
using Roster.Domain.Games;

namespace Roster.Infrastructure.Data.Repositories;

internal sealed class RuleRepository : IRuleRepository
{
    private readonly RosterDbContext _dbContext;

    public RuleRepository(RosterDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public Task<List<Rule>> GetByGameIdAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<Rule>()
            .AsNoTracking()
            .Where(r => r.GameId == gameId)
            .ToListAsync(cancellationToken);
    }

    public Task<Rule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<Rule>()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public void Add(Rule rule)
    {
        _dbContext.Set<Rule>().Add(rule);
    }

    public void Update(Rule rule)
    {
        _dbContext.Set<Rule>().Update(rule);
    }

    public void Remove(Rule rule)
    {
        _dbContext.Set<Rule>().Remove(rule);
    }
}
