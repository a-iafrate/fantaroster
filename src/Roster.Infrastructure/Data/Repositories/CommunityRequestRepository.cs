using Microsoft.EntityFrameworkCore;
using Roster.Application.Ports.Data;
using Roster.Domain.Organizers;

namespace Roster.Infrastructure.Data.Repositories;

internal sealed class CommunityRequestRepository(RosterDbContext context) : ICommunityRequestRepository
{
    public Task<CommunityRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.CommunityRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<CommunityRequest?> GetLatestByOrganizerAsync(string organizerId, CancellationToken cancellationToken = default) =>
        context.CommunityRequests
            .AsNoTracking()
            .Where(r => r.OrganizerId == organizerId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> IsApprovedAsync(string organizerId, CancellationToken cancellationToken = default) =>
        context.CommunityRequests.AnyAsync(r => r.OrganizerId == organizerId && r.Status == CommunityRequestStatus.Approved, cancellationToken);

    public async Task<List<CommunityRequest>> ListAsync(CancellationToken cancellationToken = default)
    {
        var all = await context.CommunityRequests.AsNoTracking().ToListAsync(cancellationToken);
        return all
            .OrderBy(r => r.Status == CommunityRequestStatus.Pending ? 0 : 1)
            .ThenByDescending(r => r.CreatedAt)
            .ToList();
    }

    public void Add(CommunityRequest request) => context.CommunityRequests.Add(request);
}
