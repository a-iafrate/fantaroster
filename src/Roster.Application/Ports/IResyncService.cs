using System;
using System.Threading;
using System.Threading.Tasks;
using Roster.Application.Services;

namespace Roster.Application.Ports;

public interface IResyncService
{
    Task<ResyncSummary> ResyncGameElementsAsync(Guid gameId, Guid sourceBindingId, CancellationToken cancellationToken);
}
