using Roster.Application.Ports;
using Roster.Application.Ports.Data;
using Roster.Domain.Organizers;

namespace Roster.Application.Services;

public enum CommunityRequestOutcome
{
    Created,
    AlreadyCommunity,
    AlreadyPending,
}

/// <summary>Community requests: the organizer asks, an administrator decides, the organizer is told by email.</summary>
public sealed class CommunityRequestService(
    ICommunityRequestRepository requests,
    IUnitOfWork unitOfWork,
    ICommunityNotifier notifier,
    TimeProvider timeProvider)
{
    public async Task<CommunityRequestOutcome> SubmitAsync(string organizerId, string organizerEmail, string description, string? link, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizerId);

        if (await requests.IsApprovedAsync(organizerId, cancellationToken))
        {
            return CommunityRequestOutcome.AlreadyCommunity;
        }

        var latest = await requests.GetLatestByOrganizerAsync(organizerId, cancellationToken);
        if (latest is { Status: CommunityRequestStatus.Pending })
        {
            return CommunityRequestOutcome.AlreadyPending;
        }

        requests.Add(new CommunityRequest(Guid.NewGuid(), organizerId, organizerEmail, description, link, timeProvider.GetUtcNow()));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CommunityRequestOutcome.Created;
    }

    /// <returns><c>false</c> when the request does not exist or was already decided.</returns>
    public async Task<bool> DecideAsync(Guid requestId, bool approve, string? note, string decidedBy, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(decidedBy);

        var request = await requests.GetByIdAsync(requestId, cancellationToken);
        if (request is null || request.Status != CommunityRequestStatus.Pending)
        {
            return false;
        }

        if (approve)
        {
            request.Approve(decidedBy, note, timeProvider.GetUtcNow());
        }
        else
        {
            request.Reject(decidedBy, note, timeProvider.GetUtcNow());
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await notifier.NotifyDecisionAsync(request, cancellationToken);
        return true;
    }

    public Task<List<CommunityRequest>> ListAsync(CancellationToken cancellationToken = default) => requests.ListAsync(cancellationToken);
}
