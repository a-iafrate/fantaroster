using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Roster.Application.Payments;
using Roster.Application.Ports;

namespace Roster.Infrastructure.Payments;

/// <summary>
/// PayPal through its documented REST API: Orders v2 for the one-time purchases, Billing v1 for the yearly subscription and
/// the signature check of notifications. Every call that creates something carries a request id, so a retry cannot pay twice.
/// </summary>
public sealed class PayPalGateway(HttpClient http, IOptions<PayPalOptions> options, PayPalTokenCache tokens, TimeProvider timeProvider) : IPaymentGateway
{
    private readonly PayPalOptions _options = options.Value;

    public async Task<CreatedOrder> CreateOrderAsync(OrderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var body = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    reference_id = request.ReferenceId,
                    custom_id = request.ReferenceId,
                    description = Truncate(request.Description, 120),
                    amount = new { currency_code = request.Currency, value = Money(request.Amount) },
                },
            },
            payment_source = new
            {
                paypal = new
                {
                    experience_context = new
                    {
                        brand_name = Truncate(_options.BrandName, 127),
                        user_action = "PAY_NOW",
                        shipping_preference = "NO_SHIPPING",
                        return_url = request.ReturnUrl,
                        cancel_url = request.CancelUrl,
                    },
                },
            },
        };

        using var response = await SendAsync(HttpMethod.Post, "v2/checkout/orders", body, request.RequestId, cancellationToken);
        using var json = await ReadAsync(response, cancellationToken);
        var id = json.RootElement.GetProperty("id").GetString() ?? throw new PaymentGatewayException("PayPal returned an order without an id.");
        var approve = FindLink(json.RootElement, "payer-action", "approve")
            ?? throw new PaymentGatewayException("PayPal returned an order without a place to approve it.");
        return new CreatedOrder(id, approve);
    }

    public async Task<CaptureResult> CaptureOrderAsync(string orderId, string requestId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderId);

        using var response = await SendRawAsync(HttpMethod.Post, $"v2/checkout/orders/{Uri.EscapeDataString(orderId)}/capture", "{}", requestId, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var failure = await response.Content.ReadAsStringAsync(cancellationToken);
            if ((int)response.StatusCode == 422 && failure.Contains("ORDER_ALREADY_CAPTURED", StringComparison.Ordinal))
            {
                // A repeated call: read what was captured the first time.
                using var existing = await SendAsync(HttpMethod.Get, $"v2/checkout/orders/{Uri.EscapeDataString(orderId)}", null, null, cancellationToken);
                using var existingJson = await ReadAsync(existing, cancellationToken);
                return ReadCapture(existingJson.RootElement);
            }

            throw Failure(response, failure);
        }

        using var json = await ReadAsync(response, cancellationToken);
        return ReadCapture(json.RootElement);
    }

    public async Task<CreatedSubscription> CreateSubscriptionAsync(SubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(_options.CommunityPlanId))
        {
            throw new PaymentGatewayException("The Community billing plan is not configured.");
        }

        var body = new
        {
            plan_id = _options.CommunityPlanId,
            custom_id = request.CustomId,
            application_context = new
            {
                brand_name = Truncate(_options.BrandName, 127),
                shipping_preference = "NO_SHIPPING",
                user_action = "SUBSCRIBE_NOW",
                return_url = request.ReturnUrl,
                cancel_url = request.CancelUrl,
            },
        };

        using var response = await SendAsync(HttpMethod.Post, "v1/billing/subscriptions", body, request.RequestId, cancellationToken);
        using var json = await ReadAsync(response, cancellationToken);
        var id = json.RootElement.GetProperty("id").GetString() ?? throw new PaymentGatewayException("PayPal returned a subscription without an id.");
        var approve = FindLink(json.RootElement, "approve") ?? throw new PaymentGatewayException("PayPal returned a subscription without a place to approve it.");
        return new CreatedSubscription(id, approve);
    }

    public async Task<SubscriptionState> GetSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionId);

        using var response = await SendAsync(HttpMethod.Get, $"v1/billing/subscriptions/{Uri.EscapeDataString(subscriptionId)}", null, null, cancellationToken);
        using var json = await ReadAsync(response, cancellationToken);
        var status = json.RootElement.TryGetProperty("status", out var s) ? s.GetString() : null;
        DateTimeOffset? next = null;
        if (json.RootElement.TryGetProperty("billing_info", out var billing)
            && billing.TryGetProperty("next_billing_time", out var time)
            && DateTimeOffset.TryParse(time.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            next = parsed;
        }

        return new SubscriptionState(MapStatus(status), next);
    }

    public async Task CancelSubscriptionAsync(string subscriptionId, string reason, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionId);

        using var response = await SendAsync(HttpMethod.Post, $"v1/billing/subscriptions/{Uri.EscapeDataString(subscriptionId)}/cancel", new { reason = Truncate(reason, 128) }, null, cancellationToken);
    }

    public async Task<string> CreateCommunityPlanAsync(decimal yearlyPrice, string currency, CancellationToken cancellationToken = default)
    {
        using var product = await SendAsync(
            HttpMethod.Post,
            "v1/catalogs/products",
            new { name = Truncate(string.IsNullOrWhiteSpace(_options.BrandName) ? "Community plan" : _options.BrandName + " Community", 127), type = "SERVICE", category = "SOFTWARE" },
            Guid.NewGuid().ToString("N"),
            cancellationToken);
        using var productJson = await ReadAsync(product, cancellationToken);
        var productId = productJson.RootElement.GetProperty("id").GetString()!;

        var plan = new
        {
            product_id = productId,
            name = "Community yearly",
            status = "ACTIVE",
            billing_cycles = new[]
            {
                new
                {
                    frequency = new { interval_unit = "YEAR", interval_count = 1 },
                    tenure_type = "REGULAR",
                    sequence = 1,
                    total_cycles = 0,
                    pricing_scheme = new { fixed_price = new { value = Money(yearlyPrice), currency_code = currency } },
                },
            },
            payment_preferences = new { auto_bill_outstanding = true, payment_failure_threshold = 3 },
        };
        using var created = await SendAsync(HttpMethod.Post, "v1/billing/plans", plan, Guid.NewGuid().ToString("N"), cancellationToken);
        using var planJson = await ReadAsync(created, cancellationToken);
        return planJson.RootElement.GetProperty("id").GetString()!;
    }

    public async Task<bool> VerifyWebhookAsync(WebhookRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(_options.WebhookId))
        {
            return false;
        }

        string? Header(string name) => request.Headers.FirstOrDefault(h => string.Equals(h.Key, name, StringComparison.OrdinalIgnoreCase)).Value;
        var algorithm = Header("PAYPAL-AUTH-ALGO");
        var certUrl = Header("PAYPAL-CERT-URL");
        var transmissionId = Header("PAYPAL-TRANSMISSION-ID");
        var signature = Header("PAYPAL-TRANSMISSION-SIG");
        var time = Header("PAYPAL-TRANSMISSION-TIME");
        if (algorithm is null || certUrl is null || transmissionId is null || signature is null || time is null)
        {
            return false;
        }

        // The event goes to PayPal exactly as it arrived: the signature covers its bytes.
        using var buffer = new MemoryStream();
        await using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("auth_algo", algorithm);
            writer.WriteString("cert_url", certUrl);
            writer.WriteString("transmission_id", transmissionId);
            writer.WriteString("transmission_sig", signature);
            writer.WriteString("transmission_time", time);
            writer.WriteString("webhook_id", _options.WebhookId);
            writer.WritePropertyName("webhook_event");
            using (var eventJson = JsonDocument.Parse(request.Body))
            {
                eventJson.RootElement.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        using var response = await SendRawAsync(HttpMethod.Post, "v1/notifications/verify-webhook-signature", Encoding.UTF8.GetString(buffer.ToArray()), null, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        using var json = await ReadAsync(response, cancellationToken);
        return json.RootElement.TryGetProperty("verification_status", out var status) && status.GetString() == "SUCCESS";
    }

    public PaymentWebhookEvent? ParseWebhook(string body) => PayPalWebhookParser.Parse(body);

    // ---- plumbing ----

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body, string? requestId, CancellationToken cancellationToken)
    {
        var json = body is null ? null : JsonSerializer.Serialize(body);
        var response = await SendRawAsync(method, path, json, requestId, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var failure = await response.Content.ReadAsStringAsync(cancellationToken);
            response.Dispose();
            throw Failure(response, failure);
        }

        return response;
    }

    private async Task<HttpResponseMessage> SendRawAsync(HttpMethod method, string path, string? json, string? requestId, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(method, path);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenAsync(cancellationToken));
        message.Headers.Add("Prefer", "return=representation");
        if (requestId is not null)
        {
            message.Headers.Add("PayPal-Request-Id", requestId);
        }

        if (json is not null)
        {
            message.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        try
        {
            return await http.SendAsync(message, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new PaymentGatewayException("PayPal could not be reached.", null, ex);
        }
    }

    private async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (tokens.TryGet(timeProvider.GetUtcNow(), out var cached))
        {
            return cached;
        }

        await tokens.Lock.WaitAsync(cancellationToken);
        try
        {
            if (tokens.TryGet(timeProvider.GetUtcNow(), out cached))
            {
                return cached;
            }

            using var message = new HttpRequestMessage(HttpMethod.Post, "v1/oauth2/token")
            {
                Content = new FormUrlEncodedContent([new KeyValuePair<string, string>("grant_type", "client_credentials")]),
            };
            message.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}")));
            using var response = await http.SendAsync(message, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw Failure(response, await response.Content.ReadAsStringAsync(cancellationToken));
            }

            using var json = await ReadAsync(response, cancellationToken);
            var token = json.RootElement.GetProperty("access_token").GetString() ?? throw new PaymentGatewayException("PayPal returned no access token.");
            var seconds = json.RootElement.TryGetProperty("expires_in", out var expires) ? expires.GetInt32() : 300;
            // A minute earlier than PayPal says, so a token is never used on its last second.
            tokens.Store(token, timeProvider.GetUtcNow().AddSeconds(Math.Max(30, seconds - 60)));
            return token;
        }
        finally
        {
            tokens.Lock.Release();
        }
    }

    private static async Task<JsonDocument> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
    }

    private static CaptureResult ReadCapture(JsonElement order)
    {
        var status = order.TryGetProperty("status", out var s) ? s.GetString() ?? string.Empty : string.Empty;
        if (order.TryGetProperty("purchase_units", out var units) && units.ValueKind == JsonValueKind.Array && units.GetArrayLength() > 0
            && units[0].TryGetProperty("payments", out var payments)
            && payments.TryGetProperty("captures", out var captures) && captures.ValueKind == JsonValueKind.Array && captures.GetArrayLength() > 0)
        {
            var capture = captures[0];
            var captureStatus = capture.TryGetProperty("status", out var cs) ? cs.GetString() ?? string.Empty : string.Empty;
            var money = capture.GetProperty("amount");
            var amount = decimal.Parse(money.GetProperty("value").GetString()!, NumberStyles.Number, CultureInfo.InvariantCulture);
            var currency = money.GetProperty("currency_code").GetString() ?? string.Empty;
            return new CaptureResult(captureStatus == "COMPLETED" && status == "COMPLETED", capture.GetProperty("id").GetString(), amount, currency, captureStatus);
        }

        return new CaptureResult(false, null, 0m, string.Empty, status);
    }

    private static string? FindLink(JsonElement root, params string[] relations)
    {
        if (!root.TryGetProperty("links", out var links) || links.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var relation in relations)
        {
            foreach (var link in links.EnumerateArray())
            {
                if (link.TryGetProperty("rel", out var rel) && rel.GetString() == relation && link.TryGetProperty("href", out var href))
                {
                    return href.GetString();
                }
            }
        }

        return null;
    }

    private static ProviderSubscriptionStatus MapStatus(string? status) => status switch
    {
        "ACTIVE" => ProviderSubscriptionStatus.Active,
        "APPROVED" => ProviderSubscriptionStatus.Approved,
        "SUSPENDED" => ProviderSubscriptionStatus.Suspended,
        "CANCELLED" => ProviderSubscriptionStatus.Cancelled,
        "EXPIRED" => ProviderSubscriptionStatus.Expired,
        _ => ProviderSubscriptionStatus.ApprovalPending,
    };

    private static PaymentGatewayException Failure(HttpResponseMessage response, string body)
    {
        string? debugId = response.Headers.TryGetValues("PayPal-Debug-Id", out var values) ? values.FirstOrDefault() : null;
        return new PaymentGatewayException($"PayPal answered {(int)response.StatusCode}: {Truncate(body, 300)}", debugId);
    }

    private static string Money(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Truncate(string value, int length) => value.Length <= length ? value : value[..length];
}
