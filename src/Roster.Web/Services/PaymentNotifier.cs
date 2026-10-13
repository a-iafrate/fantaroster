using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Roster.Application.Ports;
using Roster.Domain.Payments;
using Roster.Infrastructure.Data;
using Roster.Infrastructure.Email;
using Roster.Web.Brand;
using Roster.Web.Email;
using Roster.Web.Payments;

namespace Roster.Web.Services;

/// <summary>Emails the receipt of a payment to the organizer, in their preferred language and the brand look.</summary>
public sealed partial class PaymentNotifier(
    UserManager<OrganizerUser> users,
    EmailTransport email,
    IStringLocalizer<SharedResource> localizer,
    IBrandAccessor brand,
    IHttpContextAccessor httpContext,
    ILogger<PaymentNotifier> logger) : IPaymentNotifier
{
    public async Task NotifyPurchaseCompletedAsync(Purchase purchase, string gameName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(purchase);

        var user = await users.FindByIdAsync(purchase.OrganizerId);
        if (user?.Email is null)
        {
            return;
        }

        var link = BaseUrl() is { } baseUrl ? $"{baseUrl}/organizer/games/{purchase.GameId}/plan" : null;
        await SendAsync(user, link, "PaymentMailPurchase", culture => new object[]
        {
            brand.Current.Name,
            PaymentFormat.Money(purchase.Amount, purchase.Currency),
            localizer[purchase.Kind == PurchaseKind.Premium ? "PricingPlanPremium" : "PricingPlanPro"].Value,
            gameName,
            purchase.ParticipantLimit.ToString("N0", culture),
        }, purchase.ProviderOrderId, cancellationToken);
    }

    public async Task NotifySubscriptionActivatedAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        var user = await users.FindByIdAsync(subscription.OrganizerId);
        if (user?.Email is null)
        {
            return;
        }

        var link = BaseUrl() is { } baseUrl ? $"{baseUrl}/organizer/account#community" : null;
        await SendAsync(user, link, "PaymentMailSubscription", culture => new object[]
        {
            brand.Current.Name,
            subscription.PaidUntil is { } until ? until.ToLocalTime().ToString("d", culture) : string.Empty,
        }, subscription.ProviderSubscriptionId, cancellationToken);
    }

    private async Task SendAsync(OrganizerUser user, string? link, string prefix, Func<CultureInfo, object[]> arguments, string reference, CancellationToken cancellationToken)
    {
        var culture = CultureInfo.GetCultureInfo(string.IsNullOrWhiteSpace(user.PreferredCulture) ? "en" : user.PreferredCulture);
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = culture;
        string subject;
        EmailContentModel model;
        try
        {
            var values = arguments(culture);
            var current = brand.Current;
            subject = string.Format(culture, localizer[prefix + "Subject"].Value, values);
            model = new EmailContentModel(
                current.Name,
                current.ThemeColor,
                culture.TwoLetterISOLanguageName,
                string.Format(culture, localizer[prefix + "Heading"].Value, values),
                [string.Format(culture, localizer[prefix + "Body"].Value, values), string.Format(culture, localizer["PaymentMailReference"].Value, reference)],
                link is null ? null : localizer["PaymentMailButton"].Value,
                link,
                link is null ? null : localizer["MailButtonFallback"].Value,
                string.Format(culture, localizer["MailFooter"].Value, current.Name, localizer["HeroTitle"].Value));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }

        try
        {
            await email.SendAsync(user.Email!, subject, EmailMessage.ToPlainText(model), EmailMessage.ToHtml(model), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The payment is already recorded: a receipt that cannot be sent is logged, not turned into a failure.
            LogSendFailed(logger, ex, reference);
        }
    }

    private string? BaseUrl() => httpContext.HttpContext is { Request: var r } ? $"{r.Scheme}://{r.Host}" : null;

    [LoggerMessage(LogLevel.Error, "The receipt of payment {Reference} could not be emailed")]
    private static partial void LogSendFailed(ILogger logger, Exception exception, string reference);
}
