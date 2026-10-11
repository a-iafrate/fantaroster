using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Roster.Application.Ports;
using Roster.Domain.Organizers;
using Roster.Infrastructure.Data;
using Roster.Infrastructure.Email;
using Roster.Web.Brand;

namespace Roster.Web.Services;

/// <summary>Emails the organizer the decision on their community request, in their preferred language.</summary>
public sealed partial class CommunityDecisionNotifier(
    UserManager<OrganizerUser> users,
    EmailTransport email,
    IStringLocalizer<SharedResource> localizer,
    IBrandAccessor brand,
    ILogger<CommunityDecisionNotifier> logger) : ICommunityNotifier
{
    public async Task NotifyDecisionAsync(CommunityRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await users.FindByIdAsync(request.OrganizerId);
        var culture = CultureInfo.GetCultureInfo(string.IsNullOrWhiteSpace(user?.PreferredCulture) ? "en" : user.PreferredCulture);
        var approved = request.Status == CommunityRequestStatus.Approved;

        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = culture;
        string subject;
        string body;
        try
        {
            var brandName = brand.Current.Name;
            subject = string.Format(culture, localizer[approved ? "CommunityMailApprovedSubject" : "CommunityMailRejectedSubject"].Value, brandName);
            body = string.Format(culture, localizer[approved ? "CommunityMailApprovedBody" : "CommunityMailRejectedBody"].Value, brandName);
            if (!string.IsNullOrWhiteSpace(request.DecisionNote))
            {
                body += Environment.NewLine + Environment.NewLine + string.Format(culture, localizer["CommunityMailNote"].Value, request.DecisionNote);
            }
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }

        try
        {
            await email.SendAsync(request.OrganizerEmail, subject, body, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The decision is already saved: a mail that cannot be sent is logged, not turned into a failure.
            LogSendFailed(logger, ex, request.Id);
        }
    }

    [LoggerMessage(LogLevel.Error, "The decision on community request {RequestId} could not be emailed")]
    private static partial void LogSendFailed(ILogger logger, Exception exception, Guid requestId);
}
