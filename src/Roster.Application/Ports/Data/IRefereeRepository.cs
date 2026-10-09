using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roster.Domain.Referees;

namespace Roster.Application.Ports.Data;

public interface IRefereeRepository
{
    Task<IReadOnlyList<Referee>> GetByGameIdAsync(Guid gameId, CancellationToken cancellationToken = default);
    Task<Referee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(Referee referee);
    void Delete(Referee referee);
}
