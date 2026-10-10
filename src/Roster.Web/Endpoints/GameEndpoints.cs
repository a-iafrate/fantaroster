using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Roster.Application.Elements.Queries;
using Roster.Application.Services;

namespace Roster.Web.Endpoints;

public static class GameEndpoints
{
    public static IEndpointRouteBuilder MapGameEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/games/{gameId}").WithTags("Games");

        group.MapGet("/", async (
            [FromRoute] Guid gameId,
            Roster.Application.Ports.Data.IGameRepository repository,
            CancellationToken cancellationToken) =>
        {
            var game = await repository.GetByIdAsync(gameId, cancellationToken);
            return game is null
                ? Results.NotFound()
                : Results.Ok(new GameInfoResponse(game.Name, game.LineupSize, game.CaptainEnabled, game.State.ToString()));
        })
        .WithName("GetGameInfo")
        .AllowAnonymous();

        group.MapGet("/elements", async (
            [FromRoute] Guid gameId,
            ElementManagementService service,
            CancellationToken cancellationToken) =>
        {
            var query = new GetAvailableElementsQuery(gameId);
            var elements = await service.GetAvailableElementsAsync(query, cancellationToken);
            return Results.Ok(elements);
        })
        .WithName("GetAvailableElements")
        .AllowAnonymous();

        group.MapGet("/rules", async (
            [FromRoute] Guid gameId,
            RuleManagementService service,
            CancellationToken cancellationToken) =>
        {
            var rules = await service.GetRulesByGameAsync(gameId, cancellationToken);
            var dtos = rules.Select(r => new { r.Id, r.Label, r.Points, r.Category, Target = r.Target.ToString() });
            return Results.Ok(dtos);
        })
        .WithName("GetGameRules")
        .AllowAnonymous(); // TODO: In production this could be open or referee only, but elements are open too.

        app.MapGet("/api/join-codes/{joinCode}", async (
            [FromRoute] string joinCode,
            Roster.Application.Ports.Data.IGameRepository repository,
            CancellationToken cancellationToken) =>
        {
            var game = await repository.GetByJoinCodeAsync(joinCode.Trim().ToUpperInvariant(), cancellationToken);
            return game is null ? Results.NotFound() : Results.Ok(new JoinCodeResponse(game.Id));
        })
        .WithName("ResolveJoinCode")
        .AllowAnonymous();

        app.MapGet("/join/{joinCode}", async (
            [FromRoute] string joinCode,
            Roster.Application.Ports.Data.IGameRepository repository,
            CancellationToken cancellationToken) =>
        {
            var game = await repository.GetByJoinCodeAsync(joinCode, cancellationToken);
            if (game == null)
            {
                return Results.NotFound();
            }

            return Results.Redirect($"/games/{game.Id}/join");
        })
        .WithName("JoinGameByCode")
        .AllowAnonymous();

        return app;
    }
}

public sealed record JoinCodeResponse(Guid GameId);

public sealed record GameInfoResponse(string Name, int LineupSize, bool CaptainEnabled, string State);
