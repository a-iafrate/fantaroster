using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Roster.Application.Participants.Commands;
using Roster.Application.Participants.Queries;
using Roster.Application.Ports.Security;
using Roster.Application.Services;

namespace Roster.Web.Endpoints;

public static class ParticipantEndpoints
{
    public static IEndpointRouteBuilder MapParticipantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/games/{gameId}/participant").WithTags("Participants");

        group.MapPost("/join", async (
            [FromRoute] Guid gameId,
            [FromBody] JoinGameRequest request,
            ParticipantManagementService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new JoinGameCommand(gameId, request.Nickname);
                var token = await service.JoinGameAsync(command, cancellationToken);
                return Results.Ok(new { Token = token });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { Error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { Error = ex.Message });
            }
        })
        .WithName("JoinGame")
        .AllowAnonymous();

        group.MapGet("/me", async (
            [FromRoute] Guid gameId,
            HttpContext context,
            IParticipantTokenService tokenService,
            ParticipantManagementService service,
            CancellationToken cancellationToken) =>
        {
            var authHeader = context.Request.Headers.Authorization.ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return Results.Unauthorized();

            var token = authHeader.Substring("Bearer ".Length).Trim();
            if (!tokenService.ValidateToken(token, out var participantId, out var tokenGameId))
                return Results.Unauthorized();

            if (gameId != tokenGameId)
                return Results.Forbid();

            var query = new GetParticipantProfileQuery(gameId, participantId);
            var profile = await service.GetParticipantProfileAsync(query, cancellationToken);

            if (profile == null)
                return Results.NotFound();

            return Results.Ok(profile);
        })
        .WithName("GetParticipantProfile")
        .AllowAnonymous();

        group.MapGet("/lineup", async (
            [FromRoute] Guid gameId,
            HttpContext context,
            IParticipantTokenService tokenService,
            ParticipantManagementService service,
            CancellationToken cancellationToken) =>
        {
            var authHeader = context.Request.Headers.Authorization.ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return Results.Unauthorized();

            var token = authHeader.Substring("Bearer ".Length).Trim();
            if (!tokenService.ValidateToken(token, out var participantId, out var tokenGameId))
                return Results.Unauthorized();

            if (gameId != tokenGameId)
                return Results.Forbid();

            var query = new GetParticipantLineupQuery(participantId, gameId);
            var lineup = await service.GetParticipantLineupAsync(query, cancellationToken);

            if (lineup == null)
                return Results.NotFound();

            return Results.Ok(lineup);
        })
        .WithName("GetParticipantLineup")
        .AllowAnonymous();

        group.MapPut("/lineup", async (
            [FromRoute] Guid gameId,
            [FromBody] SaveLineupRequest request,
            HttpContext context,
            IParticipantTokenService tokenService,
            ParticipantManagementService service,
            CancellationToken cancellationToken) =>
        {
            var authHeader = context.Request.Headers.Authorization.ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return Results.Unauthorized();

            var token = authHeader.Substring("Bearer ".Length).Trim();
            if (!tokenService.ValidateToken(token, out var participantId, out var tokenGameId))
                return Results.Unauthorized();

            if (gameId != tokenGameId)
                return Results.Forbid();

            try
            {
                var command = new SaveLineupCommand(participantId, gameId, request.PickedElementIds, request.CaptainElementId);
                await service.SaveLineupAsync(command, cancellationToken);
                return Results.NoContent();
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { Error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { Error = ex.Message });
            }
        })
        .WithName("SaveParticipantLineup")
        .AllowAnonymous();

        group.MapGet("/leaderboard", async (
            [FromRoute] Guid gameId,
            HttpContext context,
            IParticipantTokenService tokenService,
            LeaderboardService service,
            CancellationToken cancellationToken) =>
        {
            var authHeader = context.Request.Headers.Authorization.ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return Results.Unauthorized();

            var token = authHeader.Substring("Bearer ".Length).Trim();
            if (!tokenService.ValidateToken(token, out var participantId, out var tokenGameId))
                return Results.Unauthorized();

            if (gameId != tokenGameId)
                return Results.Forbid();

            var query = new Roster.Application.Games.Queries.GetLeaderboardQuery(gameId, participantId);
            var leaderboard = await service.HandleAsync(query, cancellationToken);

            return Results.Ok(leaderboard);
        })
        .WithName("GetParticipantLeaderboard")
        .AllowAnonymous();

        group.MapGet("/activity", async (
            [FromRoute] Guid gameId,
            HttpContext context,
            IParticipantTokenService tokenService,
            ActivityFeedService service,
            CancellationToken cancellationToken) =>
        {
            var authHeader = context.Request.Headers.Authorization.ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return Results.Unauthorized();

            var token = authHeader.Substring("Bearer ".Length).Trim();
            if (!tokenService.ValidateToken(token, out var participantId, out var tokenGameId))
                return Results.Unauthorized();

            if (gameId != tokenGameId)
                return Results.Forbid();

            var query = new Roster.Application.Games.Queries.GetActivityFeedQuery(gameId);
            var feed = await service.HandleAsync(query, cancellationToken);

            return Results.Ok(feed);
        })
        .WithName("GetParticipantActivityFeed")
        .AllowAnonymous();

        return app;
    }
}

public sealed record JoinGameRequest([Required] string Nickname);
public sealed record SaveLineupRequest([Required] Guid[] PickedElementIds, Guid? CaptainElementId);
