namespace Roster.Domain.Payments;

public enum PurchaseStatus
{
    /// <summary>The order exists at the provider and the organizer is going to approve it.</summary>
    Created,
    Completed,
    Failed,
    Cancelled,
    Refunded,
}
