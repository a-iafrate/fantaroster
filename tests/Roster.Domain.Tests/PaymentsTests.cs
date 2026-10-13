using Roster.Domain.Games;
using Roster.Domain.Payments;
using Shouldly;
using Xunit;

namespace Roster.Domain.Tests;

public class PaymentsTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    private static Purchase NewPurchase(PurchaseKind kind = PurchaseKind.Premium, int limit = 200, decimal amount = 5m) =>
        new(Guid.NewGuid(), "org-1", Guid.NewGuid(), kind, limit, amount, "eur", "ORDER-1", Now);

    private static Game NewGame() => new(Guid.NewGuid(), "slug", "Game", "pack", "brand", "en", 3, true, 2m, "CODE1", Now);

    [Fact]
    public void Purchase_StartsCreatedAndKeepsTheCurrencyInUpperCase()
    {
        var purchase = NewPurchase();

        purchase.Status.ShouldBe(PurchaseStatus.Created);
        purchase.Currency.ShouldBe("EUR");
    }

    [Fact]
    public void Purchase_Complete_RecordsTheCaptureAndIsRepeatable()
    {
        var purchase = NewPurchase();

        purchase.Complete("CAPTURE-1", Now.AddMinutes(1));
        purchase.Complete("CAPTURE-2", Now.AddMinutes(2));

        purchase.Status.ShouldBe(PurchaseStatus.Completed);
        purchase.ProviderCaptureId.ShouldBe("CAPTURE-1");
        purchase.CompletedAt.ShouldBe(Now.AddMinutes(1));
    }

    [Fact]
    public void Purchase_Cancelled_CannotBeCompleted()
    {
        var purchase = NewPurchase();
        purchase.Cancel();

        Should.Throw<InvalidOperationException>(() => purchase.Complete("CAPTURE-1", Now));
    }

    [Fact]
    public void Purchase_Refund_NeedsACompletedPurchase()
    {
        var purchase = NewPurchase();
        Should.Throw<InvalidOperationException>(purchase.Refund);

        purchase.Complete("CAPTURE-1", Now);
        purchase.Refund();

        purchase.Status.ShouldBe(PurchaseStatus.Refunded);
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(100, 0)]
    [InlineData(100, -1)]
    public void Purchase_InvalidLimitOrAmount_ShouldThrow(int limit, int amount) =>
        Should.Throw<ArgumentOutOfRangeException>(() => NewPurchase(limit: limit, amount: amount));

    [Fact]
    public void Subscription_Active_GrantsThePlan()
    {
        var subscription = new Subscription(Guid.NewGuid(), "org-1", "SUB-1", Now);
        subscription.GrantsPlan(Now).ShouldBeFalse();

        subscription.Activate(Now, Now.AddYears(1));

        subscription.GrantsPlan(Now.AddMonths(6)).ShouldBeTrue();
    }

    [Fact]
    public void Subscription_Cancelled_KeepsThePlanUntilTheEndOfThePaidPeriod()
    {
        var subscription = new Subscription(Guid.NewGuid(), "org-1", "SUB-1", Now);
        subscription.Activate(Now, Now.AddYears(1));

        subscription.Cancel(Now.AddMonths(2));

        subscription.GrantsPlan(Now.AddMonths(3)).ShouldBeTrue();
        subscription.GrantsPlan(Now.AddYears(1).AddDays(1)).ShouldBeFalse();
    }

    [Fact]
    public void Subscription_SuspendedOrExpired_DoesNotGrantThePlan()
    {
        var subscription = new Subscription(Guid.NewGuid(), "org-1", "SUB-1", Now);
        subscription.Activate(Now, Now.AddYears(1));

        subscription.Suspend();
        subscription.GrantsPlan(Now).ShouldBeFalse();

        subscription.Activate(Now, Now.AddYears(1));
        subscription.Expire();
        subscription.GrantsPlan(Now).ShouldBeFalse();
    }

    [Fact]
    public void Game_ApplyPurchase_RaisesTheLimitAndKeepsTheLargerOne()
    {
        var game = NewGame();

        game.ApplyPurchase(PurchaseKind.Premium, 200);
        game.ApplyPurchase(PurchaseKind.Premium, 100);

        game.PurchasedParticipantLimit.ShouldBe(200);
        game.EffectiveParticipantLimit(50, 300, organizerIsCommunity: false).ShouldBe(200);
        game.EffectiveParticipantLimit(50, 300, organizerIsCommunity: true).ShouldBe(300);
    }

    [Fact]
    public void Game_AdministratorLimit_StillWinsOverAPurchase()
    {
        var game = NewGame();
        game.ApplyPurchase(PurchaseKind.Pro, 1000);
        game.SetParticipantLimitOverride(80, "Special");

        game.EffectiveParticipantLimit(50, 300, organizerIsCommunity: false).ShouldBe(80);
    }

    [Fact]
    public void Game_RevokePurchase_FallsBackToWhatRemains()
    {
        var game = NewGame();
        game.ApplyPurchase(PurchaseKind.Pro, 1000);

        game.RevokePurchase(PurchaseKind.Premium, 200);
        game.PurchasedParticipantLimit.ShouldBe(200);
        game.PurchasedPlan.ShouldBe(PurchaseKind.Premium);

        game.RevokePurchase(null, null);
        game.PurchasedParticipantLimit.ShouldBeNull();
        game.PurchasedPlan.ShouldBeNull();
        game.EffectiveParticipantLimit(50, 300, false).ShouldBe(50);
    }
}
