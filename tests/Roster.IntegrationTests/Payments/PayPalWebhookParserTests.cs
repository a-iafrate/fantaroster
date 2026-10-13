using Roster.Infrastructure.Payments;
using Shouldly;
using Xunit;

namespace Roster.IntegrationTests.Payments;

public sealed class PayPalWebhookParserTests
{
    [Fact]
    public void ACompletedCapture_CarriesTheOrderTheCaptureAndTheAmount()
    {
        const string body = """
            {"id":"EVT-1","event_type":"PAYMENT.CAPTURE.COMPLETED","resource":{"id":"CAP-1","amount":{"currency_code":"EUR","value":"5.00"},"supplementary_data":{"related_ids":{"order_id":"ORDER-1"}}}}
            """;

        var notification = PayPalWebhookParser.Parse(body);

        notification.ShouldNotBeNull();
        notification.Id.ShouldBe("EVT-1");
        notification.Type.ShouldBe("PAYMENT.CAPTURE.COMPLETED");
        notification.OrderId.ShouldBe("ORDER-1");
        notification.CaptureId.ShouldBe("CAP-1");
        notification.Amount.ShouldBe(5m);
        notification.Currency.ShouldBe("EUR");
    }

    [Fact]
    public void ARefund_PointsBackToItsCaptureThroughTheUpLink()
    {
        const string body = """
            {"id":"EVT-2","event_type":"PAYMENT.CAPTURE.REFUNDED","resource":{"id":"REFUND-1","links":[{"rel":"self","href":"https://api/v2/payments/refunds/REFUND-1"},{"rel":"up","href":"https://api/v2/payments/captures/CAP-1"}]}}
            """;

        PayPalWebhookParser.Parse(body)!.CaptureId.ShouldBe("CAP-1");
    }

    [Theory]
    [InlineData("""{"id":"E","event_type":"BILLING.SUBSCRIPTION.ACTIVATED","resource":{"id":"SUB-1"}}""", "SUB-1")]
    [InlineData("""{"id":"E","event_type":"BILLING.SUBSCRIPTION.CANCELLED","resource":{"id":"SUB-2"}}""", "SUB-2")]
    [InlineData("""{"id":"E","event_type":"PAYMENT.SALE.COMPLETED","resource":{"id":"SALE-1","billing_agreement_id":"SUB-3"}}""", "SUB-3")]
    public void SubscriptionEvents_CarryTheSubscription(string body, string subscription) =>
        PayPalWebhookParser.Parse(body)!.SubscriptionId.ShouldBe(subscription);

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("""{"event_type":"X"}""")]
    [InlineData("""{"id":"E"}""")]
    public void ThingsThatAreNotNotifications_AreIgnored(string body) =>
        PayPalWebhookParser.Parse(body).ShouldBeNull();
}
