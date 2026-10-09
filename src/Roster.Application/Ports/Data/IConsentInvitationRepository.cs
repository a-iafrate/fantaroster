using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roster.Domain.Elements;

namespace Roster.Application.Ports.Data;

public interface IConsentInvitationRepository
{
    Task<ConsentInvitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ConsentInvitation?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<List<ConsentInvitation>> GetByElementIdAsync(Guid elementId, CancellationToken cancellationToken = default);
    void Add(ConsentInvitation invitation);
    void Update(ConsentInvitation invitation);
}
