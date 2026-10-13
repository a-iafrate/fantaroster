using NSubstitute;
using Roster.Application.Payments;
using Roster.Application.Ports;
using Roster.Application.Ports.Data;
using Roster.Application.Services;
using Roster.Domain.Games;
using Roster.Domain.Payments;
using Shouldly;
using Xunit;

namespace Roster.Application.Tests.Payments;

public class PaymentServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    private static readonly PaymentSettings Settings = new(
        true,
        true,
        "EUR",
        [new PriceTier(100, 3m), new PriceTier(200, 5m)],
        [new PriceTier(500, 99m)],
        10m);

    private readonly IPaymentGateway _gateway = Substitute.For<IPaymentGateway>();
    private readonly IGameRepository _games = Substitute.For<IGameRepository>();
    private readonly IPurchaseRepository _purchases = Substitute.For<IPurchaseRepository>();
    private readonly ISubscriptionRepository _subscriptions = Substitute.For<ISubscriptionRepository>();
    private readonly IPaymentEventRepository _events = Substitute.For<IPaymentEventRepository>();
    private readonly ICommunityRequestRepository _requests = Substitute.For<ICommunityRequestRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPaymentNotifier _notifier = Substitute.For<IPaymentNotifier>();
    private readonly FakeTimeProvider _time = new(Now);
    private readonly Game _game;

    public PaymentServiceTests()
    {
        _game = new Game(Guid.NewGuid(), "slug", "My game", "pack", "brand", "en", 3, true, 2m, "CODE1", Now);
        _game.AssignToOrganizer("org-1");
        _games.GetByIdAsync(_game.Id, Arg.Any<CancellationToken>()).Returns(_game);
    }

    private PaymentService Service(PaymentSettings? settings = null) =>
        new(_gateway, settings ?? Settings, _games, _purchases, _subscriptions, _events, _requests, _unitOfWork, _notifier, _time);

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private Purchase ExistingPurchase(int limit = 200, decimal amount = 5m, string organizer = "org-1")
    {
        var purchase = new Purchase(Guid.NewGuid(), organizer, _game.Id, PurchaseKind.Premium, limit, amount, "EUR", "ORDER-1", Now);
        _purchases.GetByOrderIdAsync("ORDER-1", Arg.Any<CancellationToken>()).Returns(purchase);
        return purchase;
    }

    // ---- start a purchase ----

    [Fact]
    public async Task StartPurchase_CreatesTheOrderWithTheServerPrice()
    {
        _gateway.CreateOrderAsync(Arg.Any<OrderRequest>(), Arg.Any<CancellationToken>()).Returns(new CreatedOrder("ORDER-1", "https://paypal.test/approve"));

        var result = await Service().StartPurchaseAsync("org-1", _game.Id, PurchaseKind.Premium, 200, "Premium", "https://site/return", "https://site/cancel");

        result.Outcome.ShouldBe(CheckoutOutcome.Started);
        result.ApproveUrl.ShouldBe("https://paypal.test/approve");
        await _gateway.Received(1).CreateOrderAsync(Arg.Is<OrderRequest>(o => o.Amount == 5m && o.Currency == "EUR" && o.ReturnUrl == "https://site/return"), Arg.Any<CancellationToken>());
        _purchases.Received(1).Add(Arg.Is<Purchase>(p => p.Amount == 5m && p.ParticipantLimit == 200 && p.Status == PurchaseStatus.Created && p.OrganizerId == "org-1"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartPurchase_WhenPaymentsAreOff_DoesNothing()
    {
        var result = await Service(PaymentSettings.Disabled).StartPurchaseAsync("org-1", _game.Id, PurchaseKind.Premium, 200, "d", "r", "c");

        result.Outcome.ShouldBe(CheckoutOutcome.Disabled);
        await _gateway.DidNotReceiveWithAnyArgs().CreateOrderAsync(default!, default);
    }

    [Fact]
    public async Task StartPurchase_GameOfSomebodyElse_IsNotFound()
    {
        var result = await Service().StartPurchaseAsync("another", _game.Id, PurchaseKind.Premium, 200, "d", "r", "c");

        result.Outcome.ShouldBe(CheckoutOutcome.GameNotFound);
    }

    [Fact]
    public async Task StartPurchase_ATierThatDoesNotExist_IsRefused()
    {
        // The price is never taken from the client: only a real tier can be chosen.
        var result = await Service().StartPurchaseAsync("org-1", _game.Id, PurchaseKind.Premium, 150, "d", "r", "c");

        result.Outcome.ShouldBe(CheckoutOutcome.InvalidTier);
    }

    [Fact]
    public async Task StartPurchase_WhenTheGameAlreadyHasThatRoom_IsRefused()
    {
        _game.ApplyPurchase(PurchaseKind.Premium, 200);

        var result = await Service().StartPurchaseAsync("org-1", _game.Id, PurchaseKind.Premium, 100, "d", "r", "c");

        result.Outcome.ShouldBe(CheckoutOutcome.AlreadyCovered);
    }

    // ---- capture ----

    [Fact]
    public async Task CompletePurchase_CapturesGrantsTheRoomAndSendsTheReceipt()
    {
        var purchase = ExistingPurchase();
        _gateway.CaptureOrderAsync("ORDER-1", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new CaptureResult(true, "CAP-1", 5m, "EUR", "COMPLETED"));

        var result = await Service().CompletePurchaseAsync("ORDER-1", "org-1");

        result.Outcome.ShouldBe(PaymentOutcome.Completed);
        result.GameId.ShouldBe(_game.Id);
        purchase.Status.ShouldBe(PurchaseStatus.Completed);
        _game.PurchasedParticipantLimit.ShouldBe(200);
        await _notifier.Received(1).NotifyPurchaseCompletedAsync(purchase, "My game", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompletePurchase_CalledTwice_ChargesAndGrantsOnce()
    {
        ExistingPurchase();
        _gateway.CaptureOrderAsync("ORDER-1", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new CaptureResult(true, "CAP-1", 5m, "EUR", "COMPLETED"));
        var service = Service();

        await service.CompletePurchaseAsync("ORDER-1", "org-1");
        var second = await service.CompletePurchaseAsync("ORDER-1", "org-1");

        second.Outcome.ShouldBe(PaymentOutcome.AlreadyCompleted);
        await _gateway.Received(1).CaptureOrderAsync("ORDER-1", Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _notifier.Received(1).NotifyPurchaseCompletedAsync(Arg.Any<Purchase>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompletePurchase_AnAmountThatDiffers_IsNotGranted()
    {
        var purchase = ExistingPurchase();
        _gateway.CaptureOrderAsync("ORDER-1", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new CaptureResult(true, "CAP-1", 1m, "EUR", "COMPLETED"));

        var result = await Service().CompletePurchaseAsync("ORDER-1", "org-1");

        result.Outcome.ShouldBe(PaymentOutcome.Mismatch);
        purchase.Status.ShouldBe(PurchaseStatus.Failed);
        _game.PurchasedParticipantLimit.ShouldBeNull();
    }

    [Fact]
    public async Task CompletePurchase_WhenPayPalHasNotCapturedIt_GrantsNothing()
    {
        var purchase = ExistingPurchase();
        _gateway.CaptureOrderAsync("ORDER-1", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new CaptureResult(false, null, 0m, string.Empty, "PAYER_ACTION_REQUIRED"));

        (await Service().CompletePurchaseAsync("ORDER-1", "org-1")).Outcome.ShouldBe(PaymentOutcome.NotPaid);

        purchase.Status.ShouldBe(PurchaseStatus.Created);
        _game.PurchasedParticipantLimit.ShouldBeNull();
    }

    [Fact]
    public async Task CompletePurchase_ByAnotherOrganizer_IsNotFound()
    {
        ExistingPurchase();

        (await Service().CompletePurchaseAsync("ORDER-1", "someone-else")).Outcome.ShouldBe(PaymentOutcome.NotFound);

        await _gateway.DidNotReceiveWithAnyArgs().CaptureOrderAsync(default!, default!, default);
    }

    [Fact]
    public async Task CancelPurchase_MarksItCancelled()
    {
        var purchase = ExistingPurchase();

        (await Service().CancelPurchaseAsync("ORDER-1", "org-1")).ShouldBe(_game.Id);

        purchase.Status.ShouldBe(PurchaseStatus.Cancelled);
    }

    // ---- subscription ----

    [Fact]
    public async Task StartSubscription_NeedsTheApprovalOfAnAdministrator()
    {
        (await Service().StartSubscriptionAsync("org-1", "r", "c")).Outcome.ShouldBe(CheckoutOutcome.NotEligible);

        await _gateway.DidNotReceiveWithAnyArgs().CreateSubscriptionAsync(default!, default);
    }

    [Fact]
    public async Task StartSubscription_ForAnApprovedCommunity_CreatesItPending()
    {
        _requests.IsApprovedAsync("org-1", Arg.Any<CancellationToken>()).Returns(true);
        _gateway.CreateSubscriptionAsync(Arg.Any<SubscriptionRequest>(), Arg.Any<CancellationToken>()).Returns(new CreatedSubscription("SUB-1", "https://paypal.test/sub"));

        var result = await Service().StartSubscriptionAsync("org-1", "r", "c");

        result.Outcome.ShouldBe(CheckoutOutcome.Started);
        result.ApproveUrl.ShouldBe("https://paypal.test/sub");
        _subscriptions.Received(1).Add(Arg.Is<Subscription>(s => s.ProviderSubscriptionId == "SUB-1" && s.Status == SubscriptionStatus.Pending));
    }

    [Fact]
    public async Task StartSubscription_WhenAlreadyActive_IsRefused()
    {
        _requests.IsApprovedAsync("org-1", Arg.Any<CancellationToken>()).Returns(true);
        _subscriptions.HasActivePlanAsync("org-1", Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(true);

        (await Service().StartSubscriptionAsync("org-1", "r", "c")).Outcome.ShouldBe(CheckoutOutcome.AlreadyCovered);
    }

    [Fact]
    public async Task CompleteSubscription_WhenPayPalSaysActive_ActivatesItAndTellsTheOrganizer()
    {
        var subscription = new Subscription(Guid.NewGuid(), "org-1", "SUB-1", Now);
        _subscriptions.GetByProviderIdAsync("SUB-1", Arg.Any<CancellationToken>()).Returns(subscription);
        _gateway.GetSubscriptionAsync("SUB-1", Arg.Any<CancellationToken>()).Returns(new SubscriptionState(ProviderSubscriptionStatus.Active, Now.AddYears(1)));

        var result = await Service().CompleteSubscriptionAsync("SUB-1", "org-1");

        result.Outcome.ShouldBe(PaymentOutcome.Completed);
        subscription.GrantsPlan(Now).ShouldBeTrue();
        subscription.PaidUntil.ShouldBe(Now.AddYears(1));
        await _notifier.Received(1).NotifySubscriptionActivatedAsync(subscription, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteSubscription_WhenPayPalHasNotApprovedIt_StaysPending()
    {
        var subscription = new Subscription(Guid.NewGuid(), "org-1", "SUB-1", Now);
        _subscriptions.GetByProviderIdAsync("SUB-1", Arg.Any<CancellationToken>()).Returns(subscription);
        _gateway.GetSubscriptionAsync("SUB-1", Arg.Any<CancellationToken>()).Returns(new SubscriptionState(ProviderSubscriptionStatus.ApprovalPending, null));

        (await Service().CompleteSubscriptionAsync("SUB-1", "org-1")).Outcome.ShouldBe(PaymentOutcome.NotPaid);

        subscription.Status.ShouldBe(SubscriptionStatus.Pending);
    }

    [Fact]
    public async Task CancelSubscription_StopsTheRenewalButKeepsThePaidPeriod()
    {
        var subscription = new Subscription(Guid.NewGuid(), "org-1", "SUB-1", Now);
        subscription.Activate(Now, Now.AddYears(1));
        _subscriptions.GetLatestByOrganizerAsync("org-1", Arg.Any<CancellationToken>()).Returns(subscription);

        (await Service().CancelSubscriptionAsync("org-1", "reason")).ShouldBeTrue();

        await _gateway.Received(1).CancelSubscriptionAsync("SUB-1", "reason", Arg.Any<CancellationToken>());
        subscription.GrantsPlan(Now.AddMonths(1)).ShouldBeTrue();
    }

    // ---- notifications from PayPal ----

    [Fact]
    public async Task Webhook_ARepeatedNotification_IsIgnored()
    {
        _events.ExistsAsync("EVT-1", Arg.Any<CancellationToken>()).Returns(true);

        (await Service().HandleWebhookAsync(new PaymentWebhookEvent("EVT-1", "PAYMENT.CAPTURE.COMPLETED", "ORDER-1", "CAP-1", null, 5m, "EUR"))).ShouldBeFalse();

        _events.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Webhook_CaptureCompleted_GrantsTheRoomWhenTheOrganizerNeverCameBack()
    {
        var purchase = ExistingPurchase();

        (await Service().HandleWebhookAsync(new PaymentWebhookEvent("EVT-1", "PAYMENT.CAPTURE.COMPLETED", "ORDER-1", "CAP-9", null, 5m, "EUR"))).ShouldBeTrue();

        purchase.Status.ShouldBe(PurchaseStatus.Completed);
        _game.PurchasedParticipantLimit.ShouldBe(200);
        _events.Received(1).Add(Arg.Is<Domain.Payments.PaymentEvent>(e => e.Id == "EVT-1"));
    }

    [Fact]
    public async Task Webhook_CaptureCompletedWithAnotherAmount_GrantsNothing()
    {
        var purchase = ExistingPurchase();

        (await Service().HandleWebhookAsync(new PaymentWebhookEvent("EVT-1", "PAYMENT.CAPTURE.COMPLETED", "ORDER-1", "CAP-9", null, 1m, "EUR"))).ShouldBeFalse();

        purchase.Status.ShouldBe(PurchaseStatus.Created);
        _game.PurchasedParticipantLimit.ShouldBeNull();
    }

    [Fact]
    public async Task Webhook_Refund_RemovesWhatThatPaymentBoughtAndKeepsTheOthers()
    {
        var big = new Purchase(Guid.NewGuid(), "org-1", _game.Id, PurchaseKind.Pro, 500, 99m, "EUR", "ORDER-BIG", Now);
        big.Complete("CAP-BIG", Now);
        var small = new Purchase(Guid.NewGuid(), "org-1", _game.Id, PurchaseKind.Premium, 100, 3m, "EUR", "ORDER-SMALL", Now);
        small.Complete("CAP-SMALL", Now);
        _game.ApplyPurchase(PurchaseKind.Premium, 100);
        _game.ApplyPurchase(PurchaseKind.Pro, 500);
        _purchases.GetByCaptureIdAsync("CAP-BIG", Arg.Any<CancellationToken>()).Returns(big);
        _purchases.GetCompletedByGameAsync(_game.Id, Arg.Any<CancellationToken>()).Returns([big, small]);

        (await Service().HandleWebhookAsync(new PaymentWebhookEvent("EVT-2", "PAYMENT.CAPTURE.REFUNDED", null, "CAP-BIG", null, null, null))).ShouldBeTrue();

        big.Status.ShouldBe(PurchaseStatus.Refunded);
        _game.PurchasedParticipantLimit.ShouldBe(100);
        _game.PurchasedPlan.ShouldBe(PurchaseKind.Premium);
    }

    [Fact]
    public async Task Webhook_SubscriptionCancelled_IsReflectedInTheStoredSubscription()
    {
        var subscription = new Subscription(Guid.NewGuid(), "org-1", "SUB-1", Now);
        subscription.Activate(Now, Now.AddYears(1));
        _subscriptions.GetByProviderIdAsync("SUB-1", Arg.Any<CancellationToken>()).Returns(subscription);
        _gateway.GetSubscriptionAsync("SUB-1", Arg.Any<CancellationToken>()).Returns(new SubscriptionState(ProviderSubscriptionStatus.Cancelled, null));

        (await Service().HandleWebhookAsync(new PaymentWebhookEvent("EVT-3", "BILLING.SUBSCRIPTION.CANCELLED", null, null, "SUB-1", null, null))).ShouldBeTrue();

        subscription.Status.ShouldBe(SubscriptionStatus.Cancelled);
    }

    [Fact]
    public async Task Webhook_ARenewal_ExtendsThePaidPeriod()
    {
        var subscription = new Subscription(Guid.NewGuid(), "org-1", "SUB-1", Now);
        subscription.Activate(Now, Now.AddDays(1));
        _subscriptions.GetByProviderIdAsync("SUB-1", Arg.Any<CancellationToken>()).Returns(subscription);
        _gateway.GetSubscriptionAsync("SUB-1", Arg.Any<CancellationToken>()).Returns(new SubscriptionState(ProviderSubscriptionStatus.Active, Now.AddYears(1)));

        await Service().HandleWebhookAsync(new PaymentWebhookEvent("EVT-4", "PAYMENT.SALE.COMPLETED", null, null, "SUB-1", null, null));

        subscription.PaidUntil.ShouldBe(Now.AddYears(1));
    }

    // ---- the plan limit follows the subscription ----

    [Fact]
    public async Task PlanService_WithSubscriptionsOn_TheCommunityLimitNeedsAPaidSubscription()
    {
        _game.AssignToOrganizer("org-1");
        _requests.IsApprovedAsync("org-1", Arg.Any<CancellationToken>()).Returns(true);
        var plans = new PlanService(new Roster.Application.Plans.PlanLimits(50, 300), Settings, _requests, _subscriptions, _time);

        (await plans.GetEffectiveLimitAsync(_game)).ShouldBe(50);

        _subscriptions.HasActivePlanAsync("org-1", Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(true);
        (await plans.GetEffectiveLimitAsync(_game)).ShouldBe(300);
    }

    [Fact]
    public async Task PlanService_WithSubscriptionsOff_TheApprovalAloneGivesTheCommunityLimit()
    {
        _requests.IsApprovedAsync("org-1", Arg.Any<CancellationToken>()).Returns(true);
        var plans = new PlanService(new Roster.Application.Plans.PlanLimits(50, 300), PaymentSettings.Disabled, _requests, _subscriptions, _time);

        (await plans.GetEffectiveLimitAsync(_game)).ShouldBe(300);
    }

    [Fact]
    public async Task PlanService_APurchaseRaisesTheLimitOfThatGame()
    {
        _game.ApplyPurchase(PurchaseKind.Premium, 200);
        var plans = new PlanService(new Roster.Application.Plans.PlanLimits(50, 300), Settings, _requests, _subscriptions, _time);

        (await plans.GetEffectiveLimitAsync(_game)).ShouldBe(200);
    }
}
