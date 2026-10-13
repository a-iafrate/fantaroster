using Roster.Application.Payments;

namespace Roster.Application.Ports;

/// <summary>The payment provider (PayPal): one-time orders for single games and the yearly Community subscription.</summary>
public interface IPaymentGateway
{
    /// <summary>Creates an order and returns where the organizer approves it.</summary>
    Task<CreatedOrder> CreateOrderAsync(OrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>Captures the money of an order the organizer approved. Safe to call twice for the same order.</summary>
    Task<CaptureResult> CaptureOrderAsync(string orderId, string requestId, CancellationToken cancellationToken = default);

    Task<CreatedSubscription> CreateSubscriptionAsync(SubscriptionRequest request, CancellationToken cancellationToken = default);

    Task<SubscriptionState> GetSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default);

    Task CancelSubscriptionAsync(string subscriptionId, string reason, CancellationToken cancellationToken = default);

    /// <summary>Creates the product and the yearly billing plan of the Community subscription and returns the plan id to configure.</summary>
    Task<string> CreateCommunityPlanAsync(decimal yearlyPrice, string currency, CancellationToken cancellationToken = default);

    /// <summary>Whether a notification really comes from the provider.</summary>
    Task<bool> VerifyWebhookAsync(WebhookRequest request, CancellationToken cancellationToken = default);

    /// <summary>Reads a notification, or <c>null</c> when it is not one the application understands.</summary>
    PaymentWebhookEvent? ParseWebhook(string body);
}
