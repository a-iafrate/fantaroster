using System;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Roster.Application.Elements.Queries;
using Roster.Application.Services;
using Roster.DomainPacks;

namespace Roster.Web.Endpoints;

public static class GameEndpoints
{
    private const string GenericDomainPackId = "generic";

    public static IEndpointRouteBuilder MapGameEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/games/{gameId}").WithTags("Games");

        group.MapGet("/", async (
            [FromRoute] Guid gameId,
            Roster.Application.Ports.Data.IGameRepository repository,
            IDomainPackLoader domainPacks,
            CancellationToken cancellationToken) =>
        {
            var game = await repository.GetByIdAsync(gameId, cancellationToken);
            if (game is null)
            {
                return Results.NotFound();
            }

            // Context-specific words (what elements and groups are called) come from the game's domain pack.
            var pack = domainPacks.GetPack(game.DomainPackId) ?? domainPacks.GetPack(GenericDomainPackId);
            var terms = pack?.GetTerminology(CultureInfo.CurrentUICulture);

            return Results.Ok(new GameInfoResponse(
                game.Name,
                game.LineupSize,
                game.CaptainEnabled,
                game.CaptainMultiplier,
                game.State.ToString(),
                new GameTermsResponse(
                    terms?.ElementSingular ?? string.Empty,
                    terms?.ElementPlural ?? string.Empty,
                    terms?.GroupLabel ?? string.Empty)));
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

public sealed record GameInfoResponse(
    string Name,
    int LineupSize,
    bool CaptainEnabled,
    decimal CaptainMultiplier,
    string State,
    GameTermsResponse Terms);

/// <summary>Words defined by the game's domain pack for the request culture: what elements and their groups are called.</summary>
public sealed record GameTermsResponse(string ElementSingular, string ElementPlural, string GroupLabel);
