using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Roster.Infrastructure.Data;
using Roster.Infrastructure.Email;
using Roster.Web.Brand;

namespace Roster.Web.Email;

/// <summary>
/// Sends the sign-in link of organizers: a branded HTML message with a button, and a plain-text alternative, in the
/// language of the page where the link was asked for. Only the sign-in link is used; the other Identity messages are not.
/// </summary>
public sealed class MagicLinkEmailSender(
    EmailTransport transport,
    IStringLocalizer<SharedResource> localizer,
    IBrandAccessor brand) : IEmailSender<OrganizerUser>
{
    public async Task SendPasswordResetLinkAsync(OrganizerUser user, string email, string resetLink)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(resetLink);

        var current = brand.Current;
        var culture = CultureInfo.CurrentUICulture;
        var model = new EmailContentModel(
            current.Name,
            current.ThemeColor,
            culture.TwoLetterISOLanguageName,
            Format(culture, "MailLoginHeading", current.Name),
            [localizer["MailLoginIntro"].Value, localizer["MailLoginIgnore"].Value],
            localizer["MailLoginButton"].Value,
            resetLink,
            localizer["MailButtonFallback"].Value,
            Format(culture, "MailFooter", current.Name, localizer["HeroTitle"].Value));

        await transport.SendAsync(
            email,
            Format(culture, "MailLoginSubject", current.Name),
            EmailMessage.ToPlainText(model),
            EmailMessage.ToHtml(model));
    }

    public Task SendConfirmationLinkAsync(OrganizerUser user, string email, string confirmationLink) =>
        throw new NotSupportedException("Organizers sign in with a link; there is no email confirmation.");

    public Task SendPasswordResetCodeAsync(OrganizerUser user, string email, string resetCode) =>
        throw new NotSupportedException("Organizers sign in with a link; there are no codes.");

    private string Format(CultureInfo culture, string key, params object[] arguments) =>
        string.Format(culture, localizer[key].Value, arguments);
}
