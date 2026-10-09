using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Roster.Application.Ports.Data;
using Roster.Domain.Elements;

namespace Roster.Infrastructure.Data.Repositories;

public sealed class ConsentInvitationRepository : IConsentInvitationRepository
{
    private readonly RosterDbContext _context;

    public ConsentInvitationRepository(RosterDbContext context)
    {
        _context = context;
    }

    public async Task<ConsentInvitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ConsentInvitations
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<ConsentInvitation?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.ConsentInvitations
            .FirstOrDefaultAsync(c => c.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<List<ConsentInvitation>> GetByElementIdAsync(Guid elementId, CancellationToken cancellationToken = default)
    {
        return await _context.ConsentInvitations
            .Where(c => c.ElementId == elementId)
            .OrderByDescending(c => c.SentAt)
            .ToListAsync(cancellationToken);
    }

    public void Add(ConsentInvitation invitation)
    {
        _context.ConsentInvitations.Add(invitation);
    }

    public void Update(ConsentInvitation invitation)
    {
        _context.ConsentInvitations.Update(invitation);
    }
}
