using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Roster.Application.Scores.Commands;
using Roster.Application.Scores.Queries;
using Roster.Application.Services;

namespace Roster.Web.Endpoints;

public static class ScoreEndpoints
{
    public static void MapScoreEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/games/{id:guid}/score-entries")
            .WithTags("Scores");
        // .RequireAuthorization("RefereePolicy"); // TODO: Needs referee token policy

        group.MapPost("/", async (
            Guid id,
            [FromBody] AssignScoreRequest request,
            [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
            [FromHeader(Name = "Authorization")] string? authorizationHeader,
            ScoreManagementService scoreManagementService,
            Roster.Application.Ports.Data.IRefereeRepository refereeRepository,
            HttpContext context) =>
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
                return Results.BadRequest("Idempotency-Key header is required.");

            if (string.IsNullOrWhiteSpace(authorizationHeader) || !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return Results.Unauthorized();

            var token = authorizationHeader.Substring("Bearer ".Length).Trim();
            var tokenHash = RefereeTokens.Hash(token);

            var referees = await refereeRepository.GetByGameIdAsync(id, context.RequestAborted);
            var referee = referees.FirstOrDefault(r => r.InviteTokenHash == tokenHash);

            // For dev ease, if no referee is configured yet or token mismatch, allow with a warning or fallback.
            // But strict requirement says authorization via referee token.
            if (referee == null)
                return Results.Unauthorized();

            var refereeName = referee.DisplayName;

            var command = new AssignScoreCommand(
                id,
                request.ElementId,
                request.ParticipantId,
                request.RuleId,
                refereeName,
                idempotencyKey
            );

            try
            {
                var entryId = await scoreManagementService.AssignScoreAsync(command, context.RequestAborted);
                if (entryId == null)
                    return Results.Ok(new { Message = "Idempotent success" });

                return Results.Ok(new { Id = entryId.Value });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(ex.Message);
            }
        });

        group.MapPost("/{entryId:guid}/void", async (
            Guid id,
            Guid entryId,
            [FromBody] VoidScoreRequest request,
            [FromHeader(Name = "Authorization")] string? authorizationHeader,
            ScoreManagementService scoreManagementService,
            Roster.Application.Ports.Data.IRefereeRepository refereeRepository,
            HttpContext context) =>
        {
            if (string.IsNullOrWhiteSpace(authorizationHeader) || !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return Results.Unauthorized();

            var token = authorizationHeader.Substring("Bearer ".Length).Trim();
            var tokenHash = RefereeTokens.Hash(token);

            var referees = await refereeRepository.GetByGameIdAsync(id, context.RequestAborted);
            var referee = referees.FirstOrDefault(r => r.InviteTokenHash == tokenHash);
            if (referee == null)
                return Results.Unauthorized();

            var refereeName = referee.DisplayName;

            var command = new VoidScoreCommand(
                id,
                entryId,
                refereeName,
                request.Note
            );

            try
            {
                await scoreManagementService.VoidScoreAsync(command, context.RequestAborted);
                return Results.Ok();
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(ex.Message);
            }
        });

        group.MapGet("/", async (
            Guid id,
            [FromQuery] int count,
            ScoreManagementService scoreManagementService,
            HttpContext context) =>
        {
            var limit = count > 0 ? count : 20;
            var query = new GetRecentScoresQuery(id, limit);
            var results = await scoreManagementService.GetRecentScoresAsync(query, context.RequestAborted);
            return Results.Ok(results);
        });
    }
}

public sealed record AssignScoreRequest(Guid? ElementId, Guid? ParticipantId, Guid RuleId);
public sealed record VoidScoreRequest(string? Note);
