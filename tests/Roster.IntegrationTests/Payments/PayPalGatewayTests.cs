using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Roster.Application.Payments;
using Roster.Infrastructure.Payments;
using Shouldly;
using Xunit;

namespace Roster.IntegrationTests.Payments;

/// <summary>The PayPal gateway against canned answers: no network, only what the documented API says.</summary>
public sealed class PayPalGatewayTests
{
    private sealed record Seen(HttpMethod Method, string Path, string Body, IReadOnlyDictionary<string, string> Headers);

    private sealed class StubHandler(Func<Seen, (HttpStatusCode Status, string Body)> answer) : HttpMessageHandler
    {
        public List<Seen> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            var headers = request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value), StringComparer.OrdinalIgnoreCase);
            if (request.Headers.Authorization is { } authorization)
            {
                headers["Authorization"] = authorization.ToString();
            }

            var seen = new Seen(request.Method, request.RequestUri!.AbsolutePath, body, headers);
            Requests.Add(seen);
            var (status, text) = request.RequestUri.AbsolutePath == "/v1/oauth2/token"
                ? (HttpStatusCode.OK, """{"access_token":"TOKEN-1","expires_in":3600}""")
                : answer(seen);
            return new HttpResponseMessage(status) { Content = new StringContent(text, Encoding.UTF8, "application/json") };
        }
    }

    private static PayPalGateway Gateway(StubHandler handler, PayPalOptions? options = null, PayPalTokenCache? cache = null)
    {
        var settings = options ?? new PayPalOptions { ClientId = "id", ClientSecret = "secret", WebhookId = "WH-1", CommunityPlanId = "PLAN-1", BrandName = "Brand" };
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api-m.sandbox.paypal.com/") };
        return new PayPalGateway(http, Options.Create(settings), cache ?? new PayPalTokenCache(), TimeProvider.System);
    }

    private const string OrderCreated = """{"id":"ORDER-1","status":"PAYER_ACTION_REQUIRED","links":[{"rel":"self","href":"https://x/self"},{"rel":"payer-action","href":"https://www.sandbox.paypal.com/checkoutnow?token=ORDER-1"}]}""";

    private const string OrderCaptured = """
        {"id":"ORDER-1","status":"COMPLETED","purchase_units":[{"payments":{"captures":[{"id":"CAP-1","status":"COMPLETED","amount":{"currency_code":"EUR","value":"5.00"}}]}}]}
        """;

    [Fact]
    public async Task CreateOrder_SendsTheAmountTheReturnAddressesAndARequestId()
    {
        var handler = new StubHandler(_ => (HttpStatusCode.Created, OrderCreated));

        var order = await Gateway(handler).CreateOrderAsync(new OrderRequest("ref-1", 5m, "EUR", "Premium plan", "https://site/return", "https://site/cancel", "request-1"));

        order.OrderId.ShouldBe("ORDER-1");
        order.ApproveUrl.ShouldBe("https://www.sandbox.paypal.com/checkoutnow?token=ORDER-1");
        var call = handler.Requests.Single(r => r.Path == "/v2/checkout/orders");
        call.Method.ShouldBe(HttpMethod.Post);
        call.Headers["Authorization"].ShouldBe("Bearer TOKEN-1");
        call.Headers["PayPal-Request-Id"].ShouldBe("request-1");
        using var json = JsonDocument.Parse(call.Body);
        json.RootElement.GetProperty("intent").GetString().ShouldBe("CAPTURE");
        var unit = json.RootElement.GetProperty("purchase_units")[0];
        unit.GetProperty("amount").GetProperty("value").GetString().ShouldBe("5.00");
        unit.GetProperty("amount").GetProperty("currency_code").GetString().ShouldBe("EUR");
        var context = json.RootElement.GetProperty("payment_source").GetProperty("paypal").GetProperty("experience_context");
        context.GetProperty("return_url").GetString().ShouldBe("https://site/return");
        context.GetProperty("cancel_url").GetString().ShouldBe("https://site/cancel");
        context.GetProperty("brand_name").GetString().ShouldBe("Brand");
    }

    [Fact]
    public async Task TheAccessTokenIsAskedOnceAndShared()
    {
        var handler = new StubHandler(_ => (HttpStatusCode.Created, OrderCreated));
        var cache = new PayPalTokenCache();

        await Gateway(handler, cache: cache).CreateOrderAsync(new OrderRequest("a", 1m, "EUR", "d", "https://r", "https://c", "1"));
        await Gateway(handler, cache: cache).CreateOrderAsync(new OrderRequest("b", 1m, "EUR", "d", "https://r", "https://c", "2"));

        handler.Requests.Count(r => r.Path == "/v1/oauth2/token").ShouldBe(1);
    }

    [Fact]
    public async Task CaptureOrder_ReadsTheCapturedAmount()
    {
        var handler = new StubHandler(_ => (HttpStatusCode.Created, OrderCaptured));

        var capture = await Gateway(handler).CaptureOrderAsync("ORDER-1", "request-1");

        capture.Completed.ShouldBeTrue();
        capture.CaptureId.ShouldBe("CAP-1");
        capture.Amount.ShouldBe(5m);
        capture.Currency.ShouldBe("EUR");
        handler.Requests.Single(r => r.Path == "/v2/checkout/orders/ORDER-1/capture").Headers["PayPal-Request-Id"].ShouldBe("request-1");
    }

    [Fact]
    public async Task CaptureOrder_AlreadyCaptured_ReadsTheOrderInstead()
    {
        var handler = new StubHandler(seen => seen.Method == HttpMethod.Post
            ? (HttpStatusCode.UnprocessableEntity, """{"name":"UNPROCESSABLE_ENTITY","details":[{"issue":"ORDER_ALREADY_CAPTURED"}]}""")
            : (HttpStatusCode.OK, OrderCaptured));

        var capture = await Gateway(handler).CaptureOrderAsync("ORDER-1", "request-1");

        capture.Completed.ShouldBeTrue();
        capture.CaptureId.ShouldBe("CAP-1");
    }

    [Fact]
    public async Task CaptureOrder_NotCompleted_IsNotReportedAsPaid()
    {
        var handler = new StubHandler(_ => (HttpStatusCode.OK, """{"id":"ORDER-1","status":"APPROVED"}"""));

        (await Gateway(handler).CaptureOrderAsync("ORDER-1", "r")).Completed.ShouldBeFalse();
    }

    [Fact]
    public async Task AnErrorFromPayPal_BecomesAGatewayExceptionWithoutLeakingTheSecret()
    {
        var handler = new StubHandler(_ => (HttpStatusCode.BadRequest, """{"name":"INVALID_REQUEST","message":"bad"}"""));

        var ex = await Should.ThrowAsync<PaymentGatewayException>(() =>
            Gateway(handler).CreateOrderAsync(new OrderRequest("a", 1m, "EUR", "d", "https://r", "https://c", "1")));

        ex.Message.ShouldContain("400");
        ex.Message.ShouldNotContain("secret");
    }

    [Fact]
    public async Task CreateSubscription_UsesTheConfiguredPlanAndReturnsTheApprovalAddress()
    {
        var handler = new StubHandler(_ => (HttpStatusCode.Created, """{"id":"SUB-1","status":"APPROVAL_PENDING","links":[{"rel":"approve","href":"https://www.sandbox.paypal.com/webapps/billing/subscriptions?ba_token=BA-1"}]}"""));

        var created = await Gateway(handler).CreateSubscriptionAsync(new SubscriptionRequest("org-1", "https://site/return", "https://site/cancel", "request-1"));

        created.SubscriptionId.ShouldBe("SUB-1");
        created.ApproveUrl.ShouldContain("ba_token=BA-1");
        using var json = JsonDocument.Parse(handler.Requests.Single(r => r.Path == "/v1/billing/subscriptions").Body);
        json.RootElement.GetProperty("plan_id").GetString().ShouldBe("PLAN-1");
        json.RootElement.GetProperty("custom_id").GetString().ShouldBe("org-1");
    }

    [Fact]
    public async Task CreateSubscription_WithoutAPlan_Refuses()
    {
        var handler = new StubHandler(_ => (HttpStatusCode.Created, "{}"));
        var options = new PayPalOptions { ClientId = "id", ClientSecret = "secret" };

        await Should.ThrowAsync<PaymentGatewayException>(() => Gateway(handler, options).CreateSubscriptionAsync(new SubscriptionRequest("o", "https://r", "https://c", "1")));
    }

    [Theory]
    [InlineData("ACTIVE", ProviderSubscriptionStatus.Active)]
    [InlineData("APPROVED", ProviderSubscriptionStatus.Approved)]
    [InlineData("SUSPENDED", ProviderSubscriptionStatus.Suspended)]
    [InlineData("CANCELLED", ProviderSubscriptionStatus.Cancelled)]
    [InlineData("EXPIRED", ProviderSubscriptionStatus.Expired)]
    [InlineData("APPROVAL_PENDING", ProviderSubscriptionStatus.ApprovalPending)]
    public async Task GetSubscription_MapsTheStatusAndTheNextBillingTime(string status, ProviderSubscriptionStatus expected)
    {
        var handler = new StubHandler(_ => (HttpStatusCode.OK, $$$"""{"id":"SUB-1","status":"{{{status}}}","billing_info":{"next_billing_time":"2027-10-13T10:00:00Z"}}"""));

        var state = await Gateway(handler).GetSubscriptionAsync("SUB-1");

        state.Status.ShouldBe(expected);
        state.NextBillingTime.ShouldBe(new DateTimeOffset(2027, 10, 13, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task CancelSubscription_PostsTheReason()
    {
        var handler = new StubHandler(_ => (HttpStatusCode.NoContent, string.Empty));

        await Gateway(handler).CancelSubscriptionAsync("SUB-1", "No longer needed");

        var call = handler.Requests.Single(r => r.Path == "/v1/billing/subscriptions/SUB-1/cancel");
        call.Body.ShouldContain("No longer needed");
    }

    [Fact]
    public async Task CreateCommunityPlan_CreatesTheProductAndAYearlyPlan()
    {
        var handler = new StubHandler(seen => seen.Path == "/v1/catalogs/products"
            ? (HttpStatusCode.Created, """{"id":"PROD-1"}""")
            : (HttpStatusCode.Created, """{"id":"PLAN-9"}"""));

        var planId = await Gateway(handler).CreateCommunityPlanAsync(10m, "EUR");

        planId.ShouldBe("PLAN-9");
        using var plan = JsonDocument.Parse(handler.Requests.Single(r => r.Path == "/v1/billing/plans").Body);
        plan.RootElement.GetProperty("product_id").GetString().ShouldBe("PROD-1");
        var cycle = plan.RootElement.GetProperty("billing_cycles")[0];
        cycle.GetProperty("frequency").GetProperty("interval_unit").GetString().ShouldBe("YEAR");
        cycle.GetProperty("pricing_scheme").GetProperty("fixed_price").GetProperty("value").GetString().ShouldBe("10.00");
    }

    private static readonly Dictionary<string, string> SignatureHeaders = new()
    {
        ["PAYPAL-AUTH-ALGO"] = "SHA256withRSA",
        ["PAYPAL-CERT-URL"] = "https://api.sandbox.paypal.com/cert",
        ["PAYPAL-TRANSMISSION-ID"] = "tx-1",
        ["PAYPAL-TRANSMISSION-SIG"] = "sig",
        ["PAYPAL-TRANSMISSION-TIME"] = "2026-10-13T10:00:00Z",
    };

    [Fact]
    public async Task VerifyWebhook_SendsTheEventUntouchedAndTrustsOnlySuccess()
    {
        var handler = new StubHandler(_ => (HttpStatusCode.OK, """{"verification_status":"SUCCESS"}"""));
        const string eventBody = """{"id":"EVT-1","event_type":"PAYMENT.CAPTURE.COMPLETED","resource":{"id":"CAP-1"}}""";

        (await Gateway(handler).VerifyWebhookAsync(new WebhookRequest(SignatureHeaders, eventBody))).ShouldBeTrue();

        using var json = JsonDocument.Parse(handler.Requests.Single(r => r.Path == "/v1/notifications/verify-webhook-signature").Body);
        json.RootElement.GetProperty("webhook_id").GetString().ShouldBe("WH-1");
        json.RootElement.GetProperty("transmission_id").GetString().ShouldBe("tx-1");
        json.RootElement.GetProperty("webhook_event").GetProperty("id").GetString().ShouldBe("EVT-1");
    }

    [Fact]
    public async Task VerifyWebhook_AFailureOrMissingHeadersOrNoWebhookId_IsNotTrusted()
    {
        var failing = new StubHandler(_ => (HttpStatusCode.OK, """{"verification_status":"FAILURE"}"""));
        (await Gateway(failing).VerifyWebhookAsync(new WebhookRequest(SignatureHeaders, "{}"))).ShouldBeFalse();

        var any = new StubHandler(_ => (HttpStatusCode.OK, """{"verification_status":"SUCCESS"}"""));
        (await Gateway(any).VerifyWebhookAsync(new WebhookRequest(new Dictionary<string, string>(), "{}"))).ShouldBeFalse();

        var noId = new PayPalOptions { ClientId = "id", ClientSecret = "secret" };
        (await Gateway(any, noId).VerifyWebhookAsync(new WebhookRequest(SignatureHeaders, "{}"))).ShouldBeFalse();
        any.Requests.ShouldNotContain(r => r.Path == "/v1/notifications/verify-webhook-signature");
    }
}
