using Roster.Application.Payments;
using Roster.Application.Ports;
using Roster.Application.Ports.Data;
using Roster.Domain.Games;
using Roster.Domain.Payments;

namespace Roster.Application.Services;

public enum CheckoutOutcome
{
    Started,
    Disabled,
    GameNotFound,
    InvalidTier,

    /// <summary>The game already has at least this much room.</summary>
    AlreadyCovered,

    /// <summary>The organizer is not allowed to buy this (for the Community plan: not approved).</summary>
    NotEligible,
}

public sealed record CheckoutResult(CheckoutOutcome Outcome, string? ApproveUrl = null);

public enum PaymentOutcome
{
    Completed,
    AlreadyCompleted,
    NotFound,
    NotPaid,
    Mismatch,
}

public sealed record PurchaseResult(PaymentOutcome Outcome, Guid? GameId);

public sealed record SubscriptionResult(PaymentOutcome Outcome);

/// <summary>
/// Payments: one-time purchases that raise the limit of one game, and the yearly Community subscription. Amounts are set
/// here and checked again when the money is captured; every step can be repeated without paying or granting twice.
/// </summary>
public sealed class PaymentService(
    IPaymentGateway gateway,
    PaymentSettings settings,
    IGameRepository games,
    IPurchaseRepository purchases,
    ISubscriptionRepository subscriptions,
    IPaymentEventRepository events,
    ICommunityRequestRepository communityRequests,
    IUnitOfWork unitOfWork,
    IPaymentNotifier notifier,
    TimeProvider timeProvider)
{
    // ---- One-time purchases ----

    public async Task<CheckoutResult> StartPurchaseAsync(
        string organizerId,
        Guid gameId,
        PurchaseKind kind,
        int participantLimit,
        string description,
        string returnUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizerId);

        if (!settings.PurchasesEnabled)
        {
            return new CheckoutResult(CheckoutOutcome.Disabled);
        }

        var game = await games.GetByIdAsync(gameId, cancellationToken);
        if (game is null || !game.IsOwnedBy(organizerId))
        {
            return new CheckoutResult(CheckoutOutcome.GameNotFound);
        }

        var tier = settings.FindTier(kind, participantLimit);
        if (tier is null)
        {
            return new CheckoutResult(CheckoutOutcome.InvalidTier);
        }

        if ((game.PurchasedParticipantLimit ?? 0) >= tier.ParticipantLimit)
        {
            return new CheckoutResult(CheckoutOutcome.AlreadyCovered);
        }

        var purchaseId = Guid.NewGuid();
        var order = await gateway.CreateOrderAsync(
            new OrderRequest(purchaseId.ToString("N"), tier.Price, settings.Currency, description, returnUrl, cancelUrl, purchaseId.ToString("N")),
            cancellationToken);

        purchases.Add(new Purchase(purchaseId, organizerId, gameId, kind, tier.ParticipantLimit, tier.Price, settings.Currency, order.OrderId, timeProvider.GetUtcNow()));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CheckoutResult(CheckoutOutcome.Started, order.ApproveUrl);
    }

    /// <summary>Called when the organizer comes back from the provider: captures the money and grants the room.</summary>
    public async Task<PurchaseResult> CompletePurchaseAsync(string orderId, string organizerId, CancellationToken cancellationToken = default)
    {
        var purchase = await purchases.GetByOrderIdAsync(orderId, cancellationToken);
        if (purchase is null || purchase.OrganizerId != organizerId)
        {
            return new PurchaseResult(PaymentOutcome.NotFound, null);
        }

        if (purchase.Status == PurchaseStatus.Completed)
        {
            return new PurchaseResult(PaymentOutcome.AlreadyCompleted, purchase.GameId);
        }

        if (purchase.Status is not (PurchaseStatus.Created or PurchaseStatus.Failed))
        {
            return new PurchaseResult(PaymentOutcome.NotPaid, purchase.GameId);
        }

        var capture = await gateway.CaptureOrderAsync(orderId, purchase.Id.ToString("N"), cancellationToken);
        if (!capture.Completed || capture.CaptureId is null)
        {
            return new PurchaseResult(PaymentOutcome.NotPaid, purchase.GameId);
        }

        if (capture.Amount != purchase.Amount || !string.Equals(capture.Currency, purchase.Currency, StringComparison.OrdinalIgnoreCase))
        {
            purchase.Fail();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new PurchaseResult(PaymentOutcome.Mismatch, purchase.GameId);
        }

        await GrantAsync(purchase, capture.CaptureId, cancellationToken);
        return new PurchaseResult(PaymentOutcome.Completed, purchase.GameId);
    }

    /// <summary>Called when the organizer comes back from the provider without paying.</summary>
    public async Task<Guid?> CancelPurchaseAsync(string orderId, string organizerId, CancellationToken cancellationToken = default)
    {
        var purchase = await purchases.GetByOrderIdAsync(orderId, cancellationToken);
        if (purchase is null || purchase.OrganizerId != organizerId)
        {
            return null;
        }

        purchase.Cancel();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return purchase.GameId;
    }

    private async Task GrantAsync(Purchase purchase, string captureId, CancellationToken cancellationToken)
    {
        purchase.Complete(captureId, timeProvider.GetUtcNow());
        var game = await games.GetByIdAsync(purchase.GameId, cancellationToken);
        if (game is not null)
        {
            game.ApplyPurchase(purchase.Kind, purchase.ParticipantLimit);
            games.Update(game);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await notifier.NotifyPurchaseCompletedAsync(purchase, game?.Name ?? string.Empty, cancellationToken);
    }

    // ---- Community subscription ----

    public async Task<CheckoutResult> StartSubscriptionAsync(string organizerId, string returnUrl, string cancelUrl, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizerId);

        if (!settings.SubscriptionsEnabled)
        {
            return new CheckoutResult(CheckoutOutcome.Disabled);
        }

        // The authorization of an administrator comes first: the subscription is only for approved communities.
        if (!await communityRequests.IsApprovedAsync(organizerId, cancellationToken))
        {
            return new CheckoutResult(CheckoutOutcome.NotEligible);
        }

        if (await subscriptions.HasActivePlanAsync(organizerId, timeProvider.GetUtcNow(), cancellationToken))
        {
            return new CheckoutResult(CheckoutOutcome.AlreadyCovered);
        }

        var id = Guid.NewGuid();
        var created = await gateway.CreateSubscriptionAsync(new SubscriptionRequest(organizerId, returnUrl, cancelUrl, id.ToString("N")), cancellationToken);
        subscriptions.Add(new Subscription(id, organizerId, created.SubscriptionId, timeProvider.GetUtcNow()));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CheckoutResult(CheckoutOutcome.Started, created.ApproveUrl);
    }

    /// <summary>Called when the organizer comes back from the provider: reads the state of the subscription and activates it.</summary>
    public async Task<SubscriptionResult> CompleteSubscriptionAsync(string providerSubscriptionId, string organizerId, CancellationToken cancellationToken = default)
    {
        var subscription = await subscriptions.GetByProviderIdAsync(providerSubscriptionId, cancellationToken);
        if (subscription is null || subscription.OrganizerId != organizerId)
        {
            return new SubscriptionResult(PaymentOutcome.NotFound);
        }

        var wasActive = subscription.Status == SubscriptionStatus.Active;
        var active = await SyncSubscriptionAsync(subscription, cancellationToken);
        if (active && !wasActive)
        {
            await notifier.NotifySubscriptionActivatedAsync(subscription, cancellationToken);
        }

        return new SubscriptionResult(active ? (wasActive ? PaymentOutcome.AlreadyCompleted : PaymentOutcome.Completed) : PaymentOutcome.NotPaid);
    }

    /// <summary>Stops the renewal; the organizer keeps the plan until the end of the period already paid.</summary>
    public async Task<bool> CancelSubscriptionAsync(string organizerId, string reason, CancellationToken cancellationToken = default)
    {
        var subscription = await subscriptions.GetLatestByOrganizerAsync(organizerId, cancellationToken);
        if (subscription is null || subscription.Status != SubscriptionStatus.Active)
        {
            return false;
        }

        await gateway.CancelSubscriptionAsync(subscription.ProviderSubscriptionId, reason, cancellationToken);
        subscription.Cancel(timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>Makes the stored subscription say what the provider says. Returns whether it is active.</summary>
    private async Task<bool> SyncSubscriptionAsync(Subscription subscription, CancellationToken cancellationToken)
    {
        var state = await gateway.GetSubscriptionAsync(subscription.ProviderSubscriptionId, cancellationToken);
        var now = timeProvider.GetUtcNow();
        switch (state.Status)
        {
            case ProviderSubscriptionStatus.Active:
                subscription.Activate(now, state.NextBillingTime);
                break;
            case ProviderSubscriptionStatus.Suspended:
                subscription.Suspend();
                break;
            case ProviderSubscriptionStatus.Cancelled:
                subscription.Cancel(now);
                break;
            case ProviderSubscriptionStatus.Expired:
                subscription.Expire();
                break;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return subscription.Status == SubscriptionStatus.Active;
    }

    /// <summary>Creates the billing plan at the provider and returns its id, to be put in the configuration.</summary>
    public Task<string> CreateCommunityPlanAsync(CancellationToken cancellationToken = default) =>
        gateway.CreateCommunityPlanAsync(settings.CommunityYearlyPrice, settings.Currency, cancellationToken);

    // ---- Notifications from the provider ----

    /// <returns><c>true</c> when the notification was handled now; <c>false</c> when it was already handled or is not relevant.</returns>
    public async Task<bool> HandleWebhookAsync(PaymentWebhookEvent notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (await events.ExistsAsync(notification.Id, cancellationToken))
        {
            return false;
        }

        var handled = notification.Type switch
        {
            "PAYMENT.CAPTURE.COMPLETED" => await OnCaptureCompletedAsync(notification, cancellationToken),
            "PAYMENT.CAPTURE.REFUNDED" or "PAYMENT.CAPTURE.REVERSED" => await OnCaptureReturnedAsync(notification, cancellationToken),
            "PAYMENT.CAPTURE.DENIED" or "PAYMENT.CAPTURE.DECLINED" => await OnCaptureDeniedAsync(notification, cancellationToken),
            _ when notification.SubscriptionId is not null => await OnSubscriptionEventAsync(notification, cancellationToken),
            _ => false,
        };

        events.Add(new PaymentEvent(notification.Id, notification.Type, timeProvider.GetUtcNow()));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return handled;
    }

    private async Task<Purchase?> FindPurchaseAsync(PaymentWebhookEvent notification, CancellationToken cancellationToken)
    {
        if (notification.OrderId is { } orderId && await purchases.GetByOrderIdAsync(orderId, cancellationToken) is { } byOrder)
        {
            return byOrder;
        }

        return notification.CaptureId is { } captureId ? await purchases.GetByCaptureIdAsync(captureId, cancellationToken) : null;
    }

    /// <summary>The money arrived even though the organizer never came back to the site: grant the room now.</summary>
    private async Task<bool> OnCaptureCompletedAsync(PaymentWebhookEvent notification, CancellationToken cancellationToken)
    {
        var purchase = await FindPurchaseAsync(notification, cancellationToken);
        if (purchase is null || notification.CaptureId is null || purchase.Status == PurchaseStatus.Completed)
        {
            return false;
        }

        var amountMatches = notification.Amount is null || notification.Amount == purchase.Amount;
        var currencyMatches = notification.Currency is null || string.Equals(notification.Currency, purchase.Currency, StringComparison.OrdinalIgnoreCase);
        if (!amountMatches || !currencyMatches)
        {
            return false;
        }

        if (purchase.Status is not (PurchaseStatus.Created or PurchaseStatus.Failed))
        {
            return false;
        }

        await GrantAsync(purchase, notification.CaptureId, cancellationToken);
        return true;
    }

    /// <summary>A refund or a chargeback: the game loses what that payment bought, and keeps what the others bought.</summary>
    private async Task<bool> OnCaptureReturnedAsync(PaymentWebhookEvent notification, CancellationToken cancellationToken)
    {
        var purchase = await FindPurchaseAsync(notification, cancellationToken);
        if (purchase is null || purchase.Status != PurchaseStatus.Completed)
        {
            return false;
        }

        purchase.Refund();
        var game = await games.GetByIdAsync(purchase.GameId, cancellationToken);
        if (game is not null)
        {
            var remaining = (await purchases.GetCompletedByGameAsync(purchase.GameId, cancellationToken))
                .Where(p => p.Id != purchase.Id)
                .OrderByDescending(p => p.ParticipantLimit)
                .FirstOrDefault();
            game.RevokePurchase(remaining?.Kind, remaining?.ParticipantLimit);
            games.Update(game);
        }

        return true;
    }

    private async Task<bool> OnCaptureDeniedAsync(PaymentWebhookEvent notification, CancellationToken cancellationToken)
    {
        var purchase = await FindPurchaseAsync(notification, cancellationToken);
        if (purchase is null)
        {
            return false;
        }

        purchase.Fail();
        return true;
    }

    private async Task<bool> OnSubscriptionEventAsync(PaymentWebhookEvent notification, CancellationToken cancellationToken)
    {
        var subscription = await subscriptions.GetByProviderIdAsync(notification.SubscriptionId!, cancellationToken);
        if (subscription is null)
        {
            return false;
        }

        var wasActive = subscription.Status == SubscriptionStatus.Active;
        var active = await SyncSubscriptionAsync(subscription, cancellationToken);
        if (active && !wasActive)
        {
            await notifier.NotifySubscriptionActivatedAsync(subscription, cancellationToken);
        }

        return true;
    }
}
