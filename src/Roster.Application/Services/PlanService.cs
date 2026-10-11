using Roster.Application.Plans;
using Roster.Application.Ports.Data;
using Roster.Domain.Games;
using Roster.Domain.Organizers;

namespace Roster.Application.Services;

public sealed record OrganizerPlan(bool IsCommunity, int ParticipantLimit, CommunityRequest? LatestRequest);

/// <summary>What the plans allow: the participant limit of a game and the plan of an organizer.</summary>
public sealed class PlanService(PlanLimits limits, ICommunityRequestRepository communityRequests)
{
    public async Task<int> GetEffectiveLimitAsync(Game game, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(game);

        var isCommunity = game.OrganizerId is { } organizerId && await communityRequests.IsApprovedAsync(organizerId, cancellationToken);
        return game.EffectiveParticipantLimit(limits.FreeParticipantLimit, limits.CommunityParticipantLimit, isCommunity);
    }

    public async Task<OrganizerPlan> GetOrganizerPlanAsync(string organizerId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizerId);

        var latest = await communityRequests.GetLatestByOrganizerAsync(organizerId, cancellationToken);
        var isCommunity = await communityRequests.IsApprovedAsync(organizerId, cancellationToken);
        return new OrganizerPlan(isCommunity, isCommunity ? Math.Max(limits.FreeParticipantLimit, limits.CommunityParticipantLimit) : limits.FreeParticipantLimit, latest);
    }
}
