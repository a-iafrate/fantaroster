namespace Roster.Application.Payments;

public sealed record OrderRequest(string ReferenceId, decimal Amount, string Currency, string Description, string ReturnUrl, string CancelUrl, string RequestId);

public sealed record CreatedOrder(string OrderId, string ApproveUrl);

public sealed record CaptureResult(bool Completed, string? CaptureId, decimal Amount, string Currency, string Status);

public sealed record SubscriptionRequest(string CustomId, string ReturnUrl, string CancelUrl, string RequestId);

public sealed record CreatedSubscription(string SubscriptionId, string ApproveUrl);

public enum ProviderSubscriptionStatus
{
    ApprovalPending,
    Approved,
    Active,
    Suspended,
    Cancelled,
    Expired,
}

public sealed record SubscriptionState(ProviderSubscriptionStatus Status, DateTimeOffset? NextBillingTime);

public sealed record WebhookRequest(IReadOnlyDictionary<string, string> Headers, string Body);

/// <summary>What a notification from the provider says, reduced to what the application acts on.</summary>
public sealed record PaymentWebhookEvent(
    string Id,
    string Type,
    string? OrderId,
    string? CaptureId,
    string? SubscriptionId,
    decimal? Amount,
    string? Currency);
