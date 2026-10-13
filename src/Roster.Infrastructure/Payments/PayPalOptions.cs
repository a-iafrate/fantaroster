namespace Roster.Infrastructure.Payments;

/// <summary>
/// The PayPal settings (section Payments:PayPal). Payments stay off until the credentials are present; the secret belongs in
/// user secrets or Key Vault, never in a file that is committed.
/// </summary>
public sealed class PayPalOptions
{
    public const string SectionName = "Payments:PayPal";

    /// <summary>"Sandbox" (default), "Live", or "Fake" (a built-in stand-in for tests and local work, only in Development).</summary>
    public string Mode { get; set; } = "Sandbox";

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>The id of the webhook registered in the PayPal dashboard: without it notifications cannot be trusted.</summary>
    public string WebhookId { get; set; } = string.Empty;

    /// <summary>The id of the yearly Community billing plan (created once, see the administration payments page).</summary>
    public string CommunityPlanId { get; set; } = string.Empty;

    /// <summary>The name PayPal shows to the buyer.</summary>
    public string BrandName { get; set; } = string.Empty;

    public bool IsFake => string.Equals(Mode, "Fake", StringComparison.OrdinalIgnoreCase);

    public bool IsLive => string.Equals(Mode, "Live", StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether one-time purchases can be offered: the credentials are there.</summary>
    public bool PurchasesConfigured => IsFake || (!string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret));

    /// <summary>
    /// Whether the subscription can be offered: it also needs the plan and the webhook, because renewals and cancellations
    /// only reach the application through notifications.
    /// </summary>
    public bool SubscriptionsConfigured =>
        PurchasesConfigured && (IsFake || (!string.IsNullOrWhiteSpace(CommunityPlanId) && !string.IsNullOrWhiteSpace(WebhookId)));

    public Uri BaseAddress => IsLive ? new Uri("https://api-m.paypal.com/") : new Uri("https://api-m.sandbox.paypal.com/");
}
