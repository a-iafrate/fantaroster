using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roster.Domain.Sources;

namespace Roster.Application.Ports.Data;

public interface ISourceBindingRepository
{
    Task<SourceBinding?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<SourceBinding>> GetByGameIdAsync(Guid gameId, CancellationToken cancellationToken = default);
    void Add(SourceBinding binding);
    void Update(SourceBinding binding);
}
