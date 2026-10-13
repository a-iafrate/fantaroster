namespace Roster.Domain.Payments;

/// <summary>The yearly Community subscription of an organizer.</summary>
public sealed class Subscription
{
    public Guid Id { get; private set; }

    public string OrganizerId { get; private set; }

    /// <summary>The id of the subscription at the payment provider.</summary>
    public string ProviderSubscriptionId { get; private set; }

    public SubscriptionStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ActivatedAt { get; private set; }

    /// <summary>The end of the period already paid (the next billing date while active).</summary>
    public DateTimeOffset? PaidUntil { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    private Subscription()
    {
        OrganizerId = string.Empty;
        ProviderSubscriptionId = string.Empty;
    }

    public Subscription(Guid id, string organizerId, string providerSubscriptionId, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerSubscriptionId);

        Id = id;
        OrganizerId = organizerId;
        ProviderSubscriptionId = providerSubscriptionId;
        Status = SubscriptionStatus.Pending;
        CreatedAt = createdAt;
    }

    public void Activate(DateTimeOffset when, DateTimeOffset? paidUntil)
    {
        Status = SubscriptionStatus.Active;
        ActivatedAt ??= when;
        PaidUntil = paidUntil ?? PaidUntil;
        CancelledAt = null;
    }

    public void Suspend()
    {
        if (Status is SubscriptionStatus.Active)
        {
            Status = SubscriptionStatus.Suspended;
        }
    }

    public void Cancel(DateTimeOffset when)
    {
        if (Status is SubscriptionStatus.Cancelled or SubscriptionStatus.Expired)
        {
            return;
        }

        Status = SubscriptionStatus.Cancelled;
        CancelledAt = when;
    }

    public void Expire()
    {
        Status = SubscriptionStatus.Expired;
    }

    /// <summary>
    /// Whether the subscription gives the Community plan at this moment: while it is active, and after a cancellation
    /// until the end of the period that was paid for.
    /// </summary>
    public bool GrantsPlan(DateTimeOffset now) =>
        Status == SubscriptionStatus.Active
        || (Status == SubscriptionStatus.Cancelled && PaidUntil is { } until && until > now);
}
