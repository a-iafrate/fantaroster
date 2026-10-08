using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Roster.Infrastructure.Data;

namespace Roster.Web.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        group.MapGet("/verify", async (
            [FromQuery] string email,
            [FromQuery] string token,
            UserManager<OrganizerUser> userManager,
            SignInManager<OrganizerUser> signInManager,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("AuthEndpoints");
            
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return Results.Redirect("/login?error=invalid_link");
            }

            var isValid = await userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultProvider, "MagicLink", token);
            if (!isValid)
            {
                return Results.Redirect("/login?error=invalid_link");
            }

            // Invalidate the token (Optional: Identity tokens are derived from security stamp, 
            // so we can update the security stamp to invalidate previous tokens)
            await userManager.UpdateSecurityStampAsync(user);

            await signInManager.SignInAsync(user, isPersistent: true);
            
            return Results.Redirect("/organizer");
        })
        .WithName("VerifyMagicLink")
        .AllowAnonymous();

        group.MapPost("/logout", async (SignInManager<OrganizerUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.Redirect("/");
        })
        .RequireAuthorization();

        return app;
    }
}

public sealed record LoginRequest([Required][EmailAddress] string Email);
