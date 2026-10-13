using System.Collections.Concurrent;
using Roster.Application.Payments;
using Roster.Application.Ports;

namespace Roster.Infrastructure.Payments;

/// <summary>
/// A stand-in for PayPal for tests and local work (Payments:PayPal:Mode = Fake, Development only): orders are approved at
/// once, nothing leaves the machine, and the money is only a number.
/// </summary>
public sealed class FakePaymentGateway(TimeProvider timeProvider) : IPaymentGateway
{
    private readonly ConcurrentDictionary<string, OrderRequest> _orders = new();
    private readonly ConcurrentDictionary<string, bool> _cancelled = new();

    public Task<CreatedOrder> CreateOrderAsync(OrderRequest request, CancellationToken cancellationToken = default)
    {
        var id = "FAKE-ORDER-" + Guid.NewGuid().ToString("N");
        _orders[id] = request;
        return Task.FromResult(new CreatedOrder(id, Append(request.ReturnUrl, "token", id)));
    }

    public Task<CaptureResult> CaptureOrderAsync(string orderId, string requestId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_orders.TryGetValue(orderId, out var order)
            ? new CaptureResult(true, "FAKE-CAPTURE-" + orderId["FAKE-ORDER-".Length..], order.Amount, order.Currency, "COMPLETED")
            : new CaptureResult(false, null, 0m, string.Empty, "NOT_FOUND"));

    public Task<CreatedSubscription> CreateSubscriptionAsync(SubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        var id = "FAKE-SUB-" + Guid.NewGuid().ToString("N");
        return Task.FromResult(new CreatedSubscription(id, Append(request.ReturnUrl, "subscription_id", id)));
    }

    public Task<SubscriptionState> GetSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_cancelled.ContainsKey(subscriptionId)
            ? new SubscriptionState(ProviderSubscriptionStatus.Cancelled, null)
            : new SubscriptionState(ProviderSubscriptionStatus.Active, timeProvider.GetUtcNow().AddYears(1)));

    public Task CancelSubscriptionAsync(string subscriptionId, string reason, CancellationToken cancellationToken = default)
    {
        _cancelled[subscriptionId] = true;
        return Task.CompletedTask;
    }

    public Task<string> CreateCommunityPlanAsync(decimal yearlyPrice, string currency, CancellationToken cancellationToken = default) =>
        Task.FromResult("FAKE-PLAN-" + Guid.NewGuid().ToString("N"));

    public Task<bool> VerifyWebhookAsync(WebhookRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(request.Headers.Any(h => string.Equals(h.Key, "X-Fake-Signature", StringComparison.OrdinalIgnoreCase) && h.Value == "valid"));

    public PaymentWebhookEvent? ParseWebhook(string body) => PayPalWebhookParser.Parse(body);

    private static string Append(string url, string name, string value) =>
        url + (url.Contains('?', StringComparison.Ordinal) ? "&" : "?") + name + "=" + Uri.EscapeDataString(value);
}
