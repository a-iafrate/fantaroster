using System.Text.Json;
using Roster.Application.Payments;

namespace Roster.Infrastructure.Payments;

/// <summary>Reads PayPal notifications into what the application acts on.</summary>
public static class PayPalWebhookParser
{
    public static PaymentWebhookEvent? Parse(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var id = Text(root, "id");
            var type = Text(root, "event_type");
            if (id is null || type is null)
            {
                return null;
            }

            root.TryGetProperty("resource", out var resource);
            string? orderId = null;
            string? captureId = null;
            string? subscriptionId = null;
            decimal? amount = null;
            string? currency = null;

            if (type.StartsWith("PAYMENT.CAPTURE.", StringComparison.Ordinal))
            {
                orderId = Path(resource, "supplementary_data", "related_ids", "order_id");
                captureId = type == "PAYMENT.CAPTURE.REFUNDED" ? CaptureFromLinks(resource) ?? Text(resource, "id") : Text(resource, "id");
                if (resource.ValueKind == JsonValueKind.Object && resource.TryGetProperty("amount", out var money))
                {
                    currency = Text(money, "currency_code");
                    if (decimal.TryParse(Text(money, "value"), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                    {
                        amount = parsed;
                    }
                }
            }
            else if (type.StartsWith("BILLING.SUBSCRIPTION.", StringComparison.Ordinal))
            {
                subscriptionId = Text(resource, "id");
            }
            else if (type == "PAYMENT.SALE.COMPLETED")
            {
                // A renewal of a subscription: the sale points back to the subscription.
                subscriptionId = Text(resource, "billing_agreement_id");
            }

            return new PaymentWebhookEvent(id, type, orderId, captureId, subscriptionId, amount, currency);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? Text(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string? Path(JsonElement element, params string[] names)
    {
        var current = element;
        for (var i = 0; i < names.Length - 1; i++)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(names[i], out current))
            {
                return null;
            }
        }

        return Text(current, names[^1]);
    }

    /// <summary>A refund notification carries the id of the refund; the capture it belongs to is in its "up" link.</summary>
    private static string? CaptureFromLinks(JsonElement resource)
    {
        if (resource.ValueKind != JsonValueKind.Object || !resource.TryGetProperty("links", out var links) || links.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var link in links.EnumerateArray())
        {
            if (Text(link, "rel") == "up" && Text(link, "href") is { } href && href.Contains("/captures/", StringComparison.Ordinal))
            {
                return href[(href.LastIndexOf('/') + 1)..];
            }
        }

        return null;
    }
}
