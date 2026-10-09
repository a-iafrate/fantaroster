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

        return app;
    }
}
