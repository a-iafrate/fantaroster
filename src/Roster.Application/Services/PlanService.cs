using Roster.Application.Payments;
using Roster.Application.Plans;
using Roster.Application.Ports.Data;
using Roster.Domain.Games;
using Roster.Domain.Organizers;
using Roster.Domain.Payments;

namespace Roster.Application.Services;

public sealed record OrganizerPlan(
    bool IsCommunity,
    int ParticipantLimit,
    CommunityRequest? LatestRequest,
    bool IsApprovedCommunity,
    Subscription? LatestSubscription);

/// <summary>
/// What the plans allow: the participant limit of a game and the plan of an organizer. The community limit needs the
/// approval of an administrator and, once subscriptions are on, a subscription that is paid.
/// </summary>
public sealed class PlanService(
    PlanLimits limits,
    PaymentSettings payments,
    ICommunityRequestRepository communityRequests,
    ISubscriptionRepository subscriptions,
    TimeProvider timeProvider)
{
    public async Task<int> GetEffectiveLimitAsync(Game game, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(game);

        var isCommunity = game.OrganizerId is { } organizerId && await IsCommunityAsync(organizerId, cancellationToken);
        return game.EffectiveParticipantLimit(limits.FreeParticipantLimit, limits.CommunityParticipantLimit, isCommunity);
    }

    /// <summary>Whether the community limit applies to the organizer now.</summary>
    public async Task<bool> IsCommunityAsync(string organizerId, CancellationToken cancellationToken = default)
    {
        if (!await communityRequests.IsApprovedAsync(organizerId, cancellationToken))
        {
            return false;
        }

        return !payments.SubscriptionsEnabled
            || await subscriptions.HasActivePlanAsync(organizerId, timeProvider.GetUtcNow(), cancellationToken);
    }

    public async Task<OrganizerPlan> GetOrganizerPlanAsync(string organizerId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizerId);

        var latest = await communityRequests.GetLatestByOrganizerAsync(organizerId, cancellationToken);
        var approved = await communityRequests.IsApprovedAsync(organizerId, cancellationToken);
        var isCommunity = await IsCommunityAsync(organizerId, cancellationToken);
        var subscription = payments.SubscriptionsEnabled ? await subscriptions.GetLatestByOrganizerAsync(organizerId, cancellationToken) : null;
        return new OrganizerPlan(
            isCommunity,
            isCommunity ? Math.Max(limits.FreeParticipantLimit, limits.CommunityParticipantLimit) : limits.FreeParticipantLimit,
            latest,
            approved,
            subscription);
    }
}
