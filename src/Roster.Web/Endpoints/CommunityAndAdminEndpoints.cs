using Microsoft.AspNetCore.Identity;
using Roster.Application.Services;
using Roster.Infrastructure.Data;
using Roster.Web.Security;

namespace Roster.Web.Endpoints;

/// <summary>
/// Plain form posts (they work without the interactive connection): the organizer asks to be recognised as a community;
/// an administrator decides on requests and sets the participant limit of single games.
/// </summary>
public static class CommunityAndAdminEndpoints
{
    private const int MinDescription = 10;
    private const int MaxDescription = 2000;
    private const int MaxLink = 500;
    private const int MaxNote = 1000;
    private const int MaxLabel = 100;
    private const int MaxLimit = 100_000;

    public static IEndpointRouteBuilder MapCommunityAndAdminEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/organizer/account/community-request", async (
            HttpContext context,
            CommunityRequestService service,
            UserManager<OrganizerUser> users,
            CancellationToken cancellationToken) =>
        {
            var organizerId = OrganizerIdentity.GetId(context.User);
            var user = organizerId is null ? null : await users.FindByIdAsync(organizerId);
            if (user?.Email is null)
            {
                return Results.Unauthorized();
            }

            var form = await context.Request.ReadFormAsync(cancellationToken);
            var description = form["description"].ToString().Trim();
            var link = form["link"].ToString().Trim();
            if (description.Length < MinDescription || description.Length > MaxDescription || !IsValidLink(link))
            {
                return Results.Redirect("/organizer/account?community=invalid#community");
            }

            var outcome = await service.SubmitAsync(user.Id, user.Email, description, link, cancellationToken);
            var result = outcome switch
            {
                CommunityRequestOutcome.Created => "sent",
                CommunityRequestOutcome.AlreadyPending => "pending",
                _ => "already",
            };
            return Results.Redirect($"/organizer/account?community={result}#community");
        })
        .RequireAuthorization()
        .WithName("RequestCommunity");

        var admin = app.MapGroup("/admin/actions").RequireAuthorization(AdminAccess.PolicyName).WithTags("Admin");

        admin.MapPost("/requests/{requestId:guid}/decide", async (
            Guid requestId,
            HttpContext context,
            CommunityRequestService service,
            CancellationToken cancellationToken) =>
        {
            var form = await context.Request.ReadFormAsync(cancellationToken);
            var decision = form["decision"].ToString();
            var note = form["note"].ToString().Trim();
            if ((decision != "approve" && decision != "reject") || note.Length > MaxNote)
            {
                return Results.Redirect("/admin/requests?error=1");
            }

            var admin = context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "admin";
            var done = await service.DecideAsync(requestId, decision == "approve", note, admin, cancellationToken);
            return Results.Redirect(done ? "/admin/requests?decided=1" : "/admin/requests?error=1");
        })
        .WithName("DecideCommunityRequest");

        admin.MapPost("/games/{gameId:guid}/limit", async (
            Guid gameId,
            HttpContext context,
            AdminService service,
            CancellationToken cancellationToken) =>
        {
            var form = await context.Request.ReadFormAsync(cancellationToken);
            var rawLimit = form["limit"].ToString().Trim();
            var label = form["label"].ToString().Trim();
            int? limit = null;
            if (rawLimit.Length > 0)
            {
                if (!int.TryParse(rawLimit, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var parsed) || parsed < 1 || parsed > MaxLimit)
                {
                    return Results.Redirect("/admin/games?error=1");
                }

                limit = parsed;
            }

            if (label.Length > MaxLabel)
            {
                return Results.Redirect("/admin/games?error=1");
            }

            var done = await service.SetGameLimitAsync(gameId, limit, label, cancellationToken);
            return Results.Redirect(done ? "/admin/games?saved=1" : "/admin/games?error=1");
        })
        .WithName("SetGameParticipantLimit");

        return app;
    }

    private static bool IsValidLink(string link) =>
        link.Length == 0
        || (link.Length <= MaxLink && Uri.TryCreate(link, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));
}
