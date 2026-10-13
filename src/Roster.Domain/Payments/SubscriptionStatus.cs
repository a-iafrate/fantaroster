namespace Roster.Domain.Payments;

public enum SubscriptionStatus
{
    /// <summary>Created at the provider; the organizer has not approved it yet.</summary>
    Pending,
    Active,
    Suspended,
    Cancelled,
    Expired,
}
