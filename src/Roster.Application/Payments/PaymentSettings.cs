using Roster.Domain.Payments;

namespace Roster.Application.Payments;

/// <summary>One step of the price list: what a game with up to this many participants costs.</summary>
public sealed record PriceTier(int ParticipantLimit, decimal Price);

/// <summary>
/// What can be bought and for how much. Payments are off unless the payment provider is fully configured: then nothing
/// can be bought and the plans work as before the payments existed. Prices are always decided here, on the server.
/// </summary>
public sealed record PaymentSettings(
    bool PurchasesEnabled,
    bool SubscriptionsEnabled,
    string Currency,
    IReadOnlyList<PriceTier> PremiumTiers,
    IReadOnlyList<PriceTier> ProTiers,
    decimal CommunityYearlyPrice)
{
    public static PaymentSettings Disabled { get; } = new(false, false, "EUR", [], [], 0m);

    public IReadOnlyList<PriceTier> TiersFor(PurchaseKind kind) => kind == PurchaseKind.Premium ? PremiumTiers : ProTiers;

    /// <summary>The tier with exactly this participant limit, or <c>null</c>: the client can only choose among the real ones.</summary>
    public PriceTier? FindTier(PurchaseKind kind, int participantLimit) =>
        TiersFor(kind).FirstOrDefault(t => t.ParticipantLimit == participantLimit);
}
