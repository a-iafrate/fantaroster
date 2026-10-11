using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Roster.Application.Ports;
using Roster.Domain.Organizers;
using Roster.Infrastructure.Data;
using Roster.Infrastructure.Email;
using Roster.Web.Brand;
using Roster.Web.Email;

namespace Roster.Web.Services;

/// <summary>Emails the organizer the decision on their community request, in their preferred language and the brand look.</summary>
public sealed partial class CommunityDecisionNotifier(
    UserManager<OrganizerUser> users,
    EmailTransport email,
    IStringLocalizer<SharedResource> localizer,
    IBrandAccessor brand,
    IHttpContextAccessor httpContext,
    ILogger<CommunityDecisionNotifier> logger) : ICommunityNotifier
{
    public async Task NotifyDecisionAsync(CommunityRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await users.FindByIdAsync(request.OrganizerId);
        var culture = CultureInfo.GetCultureInfo(string.IsNullOrWhiteSpace(user?.PreferredCulture) ? "en" : user.PreferredCulture);
        var approved = request.Status == CommunityRequestStatus.Approved;
        var accountUrl = httpContext.HttpContext is { Request: var r } ? $"{r.Scheme}://{r.Host}/organizer/account" : null;

        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = culture;
        string subject;
        EmailContentModel model;
        try
        {
            var current = brand.Current;
            var prefix = approved ? "CommunityMailApproved" : "CommunityMailRejected";
            var paragraphs = new List<string> { Format(culture, prefix + "Body", current.Name) };
            if (!string.IsNullOrWhiteSpace(request.DecisionNote))
            {
                paragraphs.Add(Format(culture, "CommunityMailNote", request.DecisionNote));
            }

            subject = Format(culture, prefix + "Subject", current.Name);
            model = new EmailContentModel(
                current.Name,
                current.ThemeColor,
                culture.TwoLetterISOLanguageName,
                Format(culture, prefix + "Heading", current.Name),
                paragraphs,
                accountUrl is null ? null : localizer["CommunityMailButton"].Value,
                accountUrl,
                accountUrl is null ? null : localizer["MailButtonFallback"].Value,
                Format(culture, "MailFooter", current.Name, localizer["HeroTitle"].Value));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }

        try
        {
            await email.SendAsync(request.OrganizerEmail, subject, EmailMessage.ToPlainText(model), EmailMessage.ToHtml(model), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The decision is already saved: a mail that cannot be sent is logged, not turned into a failure.
            LogSendFailed(logger, ex, request.Id);
        }
    }

    private string Format(CultureInfo culture, string key, params object[] arguments) =>
        string.Format(culture, localizer[key].Value, arguments);

    [LoggerMessage(LogLevel.Error, "The decision on community request {RequestId} could not be emailed")]
    private static partial void LogSendFailed(ILogger logger, Exception exception, Guid requestId);
}
