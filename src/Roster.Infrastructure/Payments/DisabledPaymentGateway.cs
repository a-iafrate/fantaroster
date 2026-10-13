using Roster.Application.Payments;
using Roster.Application.Ports;

namespace Roster.Infrastructure.Payments;

/// <summary>Used when the payment provider is not configured: nothing can be bought, and every call says so.</summary>
public sealed class DisabledPaymentGateway : IPaymentGateway
{
    private static PaymentGatewayException Off() => new("Payments are not configured.");

    public Task<CreatedOrder> CreateOrderAsync(OrderRequest request, CancellationToken cancellationToken = default) => throw Off();

    public Task<CaptureResult> CaptureOrderAsync(string orderId, string requestId, CancellationToken cancellationToken = default) => throw Off();

    public Task<CreatedSubscription> CreateSubscriptionAsync(SubscriptionRequest request, CancellationToken cancellationToken = default) => throw Off();

    public Task<SubscriptionState> GetSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default) => throw Off();

    public Task CancelSubscriptionAsync(string subscriptionId, string reason, CancellationToken cancellationToken = default) => throw Off();

    public Task<string> CreateCommunityPlanAsync(decimal yearlyPrice, string currency, CancellationToken cancellationToken = default) => throw Off();

    public Task<bool> VerifyWebhookAsync(WebhookRequest request, CancellationToken cancellationToken = default) => Task.FromResult(false);

    public PaymentWebhookEvent? ParseWebhook(string body) => null;
}
