using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Roster.Application.Ports;

namespace Roster.Infrastructure.Payments;

public static class PaymentsServiceCollectionExtensions
{
    /// <summary>
    /// Registers the payment gateway the settings call for: PayPal when it is configured, the built-in stand-in when asked
    /// for (and only in Development), and otherwise one that refuses everything, so payments stay off.
    /// </summary>
    public static IServiceCollection AddPayments(this IServiceCollection services, IConfiguration configuration, bool allowFake)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<PayPalOptions>(configuration.GetSection(PayPalOptions.SectionName));
        var options = configuration.GetSection(PayPalOptions.SectionName).Get<PayPalOptions>() ?? new PayPalOptions();

        if (options.IsFake && allowFake)
        {
            services.AddSingleton<IPaymentGateway, FakePaymentGateway>();
        }
        else if (!options.IsFake && options.PurchasesConfigured)
        {
            services.AddSingleton<PayPalTokenCache>();
            services.AddHttpClient<IPaymentGateway, PayPalGateway>((provider, client) =>
                    client.BaseAddress = provider.GetRequiredService<IOptions<PayPalOptions>>().Value.BaseAddress)
                .AddStandardResilienceHandler();
        }
        else
        {
            services.AddSingleton<IPaymentGateway, DisabledPaymentGateway>();
        }

        return services;
    }

    /// <summary>Whether payments can be offered with this configuration (purchases, and the subscription on top).</summary>
    public static (bool Purchases, bool Subscriptions) PaymentCapabilities(IConfiguration configuration, bool allowFake)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration.GetSection(PayPalOptions.SectionName).Get<PayPalOptions>() ?? new PayPalOptions();
        if (options.IsFake && !allowFake)
        {
            return (false, false);
        }

        return (options.PurchasesConfigured, options.SubscriptionsConfigured);
    }
}
