using System.Globalization;
using Roster.Application.Payments;
using Roster.Infrastructure.Payments;

namespace Roster.Web.Payments;

/// <summary>
/// Builds what can be bought from the configuration. The price list has defaults (indicative, to be validated with the
/// pilot) and is changed under Payments: PremiumTiers, ProTiers, CommunityYearlyPrice, Currency. Without a complete PayPal
/// configuration nothing is for sale.
/// </summary>
public static class PaymentCatalog
{
    public sealed class TierOption
    {
        public int ParticipantLimit { get; set; }

        public decimal Price { get; set; }
    }

    private static readonly TierOption[] DefaultPremium = [new() { ParticipantLimit = 100, Price = 3m }, new() { ParticipantLimit = 200, Price = 5m }, new() { ParticipantLimit = 300, Price = 9m }];

    private static readonly TierOption[] DefaultPro = [new() { ParticipantLimit = 500, Price = 99m }, new() { ParticipantLimit = 1000, Price = 199m }, new() { ParticipantLimit = 2000, Price = 299m }];

    public static PaymentSettings FromConfiguration(IConfiguration configuration, bool allowFake)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var (purchases, subscriptions) = PaymentsServiceCollectionExtensions.PaymentCapabilities(configuration, allowFake);
        if (!purchases)
        {
            return PaymentSettings.Disabled;
        }

        var currency = configuration["Payments:Currency"] is { Length: 3 } code ? code.ToUpperInvariant() : "EUR";
        var yearly = decimal.TryParse(configuration["Payments:CommunityYearlyPrice"], NumberStyles.Number, CultureInfo.InvariantCulture, out var price) && price > 0 ? price : 10m;
        return new PaymentSettings(true, subscriptions, currency, Tiers(configuration, "Payments:PremiumTiers", DefaultPremium), Tiers(configuration, "Payments:ProTiers", DefaultPro), yearly);
    }

    private static List<PriceTier> Tiers(IConfiguration configuration, string section, TierOption[] defaults)
    {
        var configured = configuration.GetSection(section).Get<List<TierOption>>();
        var source = configured is { Count: > 0 } ? configured : [.. defaults];
        return source
            .Where(t => t.ParticipantLimit > 0 && t.Price > 0)
            .OrderBy(t => t.ParticipantLimit)
            .Select(t => new PriceTier(t.ParticipantLimit, t.Price))
            .ToList();
    }
}

public static class PaymentFormat
{
    /// <summary>An amount with its currency code, in the culture of the reader: "5,00 EUR".</summary>
    public static string Money(decimal amount, string currency) =>
        string.Format(CultureInfo.CurrentCulture, "{0:N2} {1}", amount, currency);
}
