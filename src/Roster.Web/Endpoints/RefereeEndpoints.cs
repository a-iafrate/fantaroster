using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Roster.Application.Ports.Data;
using Roster.Application.Services;

namespace Roster.Web.Endpoints;

public static class RefereeEndpoints
{
    public static IEndpointRouteBuilder MapRefereeEndpoints(this IEndpointRouteBuilder app)
    {
        // Lets the referee console check an invite token before it shows anything.
        app.MapGet("/api/games/{gameId:guid}/referee/me", async (
            Guid gameId,
            HttpContext context,
            IRefereeRepository refereeRepository) =>
        {
            var header = context.Request.Headers.Authorization.ToString();
            if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Unauthorized();
            }

            var tokenHash = RefereeTokens.Hash(header["Bearer ".Length..].Trim());
            var referees = await refereeRepository.GetByGameIdAsync(gameId, context.RequestAborted);
            var referee = referees.FirstOrDefault(r => r.InviteTokenHash == tokenHash);

            return referee is null ? Results.Unauthorized() : Results.Ok(new RefereeInfoResponse(referee.DisplayName));
        })
        .WithTags("Referees")
        .WithName("GetRefereeInfo")
        .AllowAnonymous();

        return app;
    }
}

public sealed record RefereeInfoResponse(string DisplayName);
