using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Roster.Domain.Games;
using Roster.Domain.Payments;
using Roster.Infrastructure.Data;
using Shouldly;
using Xunit;

namespace Roster.IntegrationTests.Payments;

/// <summary>The notifications of the payment provider, through the real endpoint and database (the provider is the stand-in).</summary>
public sealed class PaymentEndpointsTests : IClassFixture<RosterWebFactory>
{
    private readonly RosterWebFactory _factory;

    public PaymentEndpointsTests(RosterWebFactory factory)
    {
        _factory = factory;
    }

    private async Task<(Guid GameId, string OrderId)> SeedAsync(IServiceProvider services, PurchaseStatus status = PurchaseStatus.Created)
    {
        var db = services.GetRequiredService<RosterDbContext>();
        var id = Guid.NewGuid();
        var game = new Game(id, $"slug-{id:N}", "Paid game", "pack", "brand", "en", 3, true, 2m, id.ToString("N")[..8].ToUpperInvariant(), DateTimeOffset.UtcNow);
        game.AssignToOrganizer("org-" + id.ToString("N"));
        var orderId = "ORDER-" + id.ToString("N");
        var purchase = new Purchase(Guid.NewGuid(), game.OrganizerId!, id, PurchaseKind.Premium, 200, 5m, "EUR", orderId, DateTimeOffset.UtcNow);
        if (status == PurchaseStatus.Completed)
        {
            purchase.Complete("CAP-" + id.ToString("N"), DateTimeOffset.UtcNow);
            game.ApplyPurchase(PurchaseKind.Premium, 200);
        }

        db.Games.Add(game);
        db.Purchases.Add(purchase);
        await db.SaveChangesAsync();
        return (id, orderId);
    }

    private static HttpRequestMessage Notification(string json, string signature = "valid")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments/paypal/webhook") { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        request.Headers.Add("X-Fake-Signature", signature);
        return request;
    }

    private static string CaptureCompleted(string eventId, string orderId, string amount = "5.00") =>
        """{"id":"EVENT","event_type":"PAYMENT.CAPTURE.COMPLETED","resource":{"id":"CAP-EVENT","amount":{"currency_code":"EUR","value":"AMOUNT"},"supplementary_data":{"related_ids":{"order_id":"ORDER"}}}}"""
            .Replace("EVENT", eventId, StringComparison.Ordinal)
            .Replace("AMOUNT", amount, StringComparison.Ordinal)
            .Replace("ORDER", orderId, StringComparison.Ordinal);

    [Fact]
    public async Task ANotificationThatIsNotSigned_IsRefusedAndChangesNothing()
    {
        using var scope = _factory.Services.CreateScope();
        var (gameId, orderId) = await SeedAsync(scope.ServiceProvider);

        using var response = await _factory.CreateClient().SendAsync(Notification(CaptureCompleted("EVT-" + Guid.NewGuid().ToString("N"), orderId), signature: "forged"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await scope.ServiceProvider.GetRequiredService<RosterDbContext>().Games.AsNoTracking().SingleAsync(g => g.Id == gameId)).PurchasedParticipantLimit.ShouldBeNull();
    }

    [Fact]
    public async Task ACompletedCapture_RaisesTheLimitOfTheGame_AndRepeatingItDoesNothingMore()
    {
        using var scope = _factory.Services.CreateScope();
        var (gameId, orderId) = await SeedAsync(scope.ServiceProvider);
        var eventId = "EVT-" + Guid.NewGuid().ToString("N");
        var client = _factory.CreateClient();

        using var first = await client.SendAsync(Notification(CaptureCompleted(eventId, orderId)));
        using var again = await client.SendAsync(Notification(CaptureCompleted(eventId, orderId)));

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        again.StatusCode.ShouldBe(HttpStatusCode.OK);
        var db = scope.ServiceProvider.GetRequiredService<RosterDbContext>();
        var game = await db.Games.AsNoTracking().SingleAsync(g => g.Id == gameId);
        game.PurchasedParticipantLimit.ShouldBe(200);
        game.PurchasedPlan.ShouldBe(PurchaseKind.Premium);
        (await db.Purchases.AsNoTracking().SingleAsync(p => p.ProviderOrderId == orderId)).Status.ShouldBe(PurchaseStatus.Completed);
        (await db.PaymentEvents.CountAsync(e => e.Id == eventId)).ShouldBe(1);
    }

    [Fact]
    public async Task ACaptureForAnotherAmount_IsAcknowledgedButGrantsNothing()
    {
        using var scope = _factory.Services.CreateScope();
        var (gameId, orderId) = await SeedAsync(scope.ServiceProvider);

        using var response = await _factory.CreateClient().SendAsync(Notification(CaptureCompleted("EVT-" + Guid.NewGuid().ToString("N"), orderId, amount: "1.00")));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await scope.ServiceProvider.GetRequiredService<RosterDbContext>().Games.AsNoTracking().SingleAsync(g => g.Id == gameId)).PurchasedParticipantLimit.ShouldBeNull();
    }

    [Fact]
    public async Task ARefund_TakesTheRoomAwayAgain()
    {
        using var scope = _factory.Services.CreateScope();
        var (gameId, _) = await SeedAsync(scope.ServiceProvider, PurchaseStatus.Completed);
        var captureId = "CAP-" + gameId.ToString("N");
        var refund = $$$"""{"id":"EVT-{{{Guid.NewGuid():N}}}","event_type":"PAYMENT.CAPTURE.REFUNDED","resource":{"id":"REFUND-1","links":[{"rel":"up","href":"https://api/v2/payments/captures/{{{captureId}}}"}]}}""";

        using var response = await _factory.CreateClient().SendAsync(Notification(refund));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var game = await scope.ServiceProvider.GetRequiredService<RosterDbContext>().Games.AsNoTracking().SingleAsync(g => g.Id == gameId);
        game.PurchasedParticipantLimit.ShouldBeNull();
        game.EffectiveParticipantLimit(50, 300, false).ShouldBe(50);
    }

    [Fact]
    public async Task TheOrganizerPagesOfPaymentsNeedASignIn()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var checkout = await client.PostAsync($"/organizer/actions/games/{Guid.NewGuid()}/checkout", new FormUrlEncodedContent([new KeyValuePair<string, string>("kind", "premium")]));
        using var returned = await client.GetAsync("/billing/return/order?token=ORDER-1");

        checkout.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        checkout.Headers.Location!.ToString().ShouldContain("/login");
        returned.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        returned.Headers.Location!.ToString().ShouldContain("/login");
    }
}
