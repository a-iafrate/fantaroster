using Roster.Domain.Organizers;

namespace Roster.Application.Ports.Data;

public interface ICommunityRequestRepository
{
    Task<CommunityRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The most recent request of the organizer, if any.</summary>
    Task<CommunityRequest?> GetLatestByOrganizerAsync(string organizerId, CancellationToken cancellationToken = default);

    /// <summary>Whether the organizer has an approved request.</summary>
    Task<bool> IsApprovedAsync(string organizerId, CancellationToken cancellationToken = default);

    /// <summary>All requests, pending first and then the newest.</summary>
    Task<List<CommunityRequest>> ListAsync(CancellationToken cancellationToken = default);

    void Add(CommunityRequest request);
}
