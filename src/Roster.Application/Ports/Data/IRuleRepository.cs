using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roster.Domain.Games;

namespace Roster.Application.Ports.Data;

public interface IRuleRepository
{
    Task<List<Rule>> GetByGameIdAsync(Guid gameId, CancellationToken cancellationToken = default);
    Task<Rule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(Rule rule);
    void Update(Rule rule);
    void Remove(Rule rule);
}
