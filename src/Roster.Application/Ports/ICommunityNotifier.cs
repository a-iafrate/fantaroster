using Roster.Domain.Organizers;

namespace Roster.Application.Ports;

/// <summary>Tells the organizer that their community request was approved or rejected (by email).</summary>
public interface ICommunityNotifier
{
    /// <summary>Sends the decision. A failure to send must not fail the decision: implementations log it.</summary>
    Task NotifyDecisionAsync(CommunityRequest request, CancellationToken cancellationToken = default);
}
