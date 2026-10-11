using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Roster.Infrastructure.Data;
using Roster.Infrastructure.Security;
using Roster.Web.Security;

namespace Roster.Web.Endpoints;

/// <summary>The organizer's data rights: download everything, delete the account with its games.</summary>
public static class AccountEndpoints
{
    private static readonly JsonSerializerOptions ExportOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/organizer/account").WithTags("Account").RequireAuthorization();

        group.MapGet("/export", async (
            HttpContext context,
            OrganizerAccountService service,
            TimeProvider time,
            CancellationToken cancellationToken) =>
        {
            var organizerId = OrganizerIdentity.GetId(context.User);
            if (organizerId is null)
            {
                return Results.Unauthorized();
            }

            var export = await service.ExportAsync(organizerId, time, cancellationToken);
            if (export is null)
            {
                return Results.NotFound();
            }

            context.Response.Headers.CacheControl = "no-store";
            return Results.File(
                JsonSerializer.SerializeToUtf8Bytes(export, ExportOptions),
                "application/json",
                $"my-data-{export.ExportedAt:yyyyMMdd}.json");
        })
        .WithName("ExportOrganizerData");

        group.MapPost("/delete", async (
            HttpContext context,
            OrganizerAccountService service,
            UserManager<OrganizerUser> userManager,
            SignInManager<OrganizerUser> signInManager,
            CancellationToken cancellationToken) =>
        {
            var organizerId = OrganizerIdentity.GetId(context.User);
            var user = organizerId is null ? null : await userManager.FindByIdAsync(organizerId);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            // Typing the address is the deliberate step: the account and its games cannot be brought back.
            var form = await context.Request.ReadFormAsync(cancellationToken);
            var confirmation = form["confirmEmail"].ToString().Trim();
            if (!string.Equals(confirmation, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                return Results.Redirect("/organizer/account?error=confirm");
            }

            if (!await service.DeleteAccountAsync(user.Id, cancellationToken))
            {
                return Results.Redirect("/organizer/account?error=failed");
            }

            await signInManager.SignOutAsync();
            return Results.Redirect("/?deleted=1");
        })
        .WithName("DeleteOrganizerAccount");

        return app;
    }
}
