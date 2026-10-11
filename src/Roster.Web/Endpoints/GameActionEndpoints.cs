using Microsoft.Extensions.Localization;
using Roster.Application.Ports.Data;
using Roster.Application.Services;
using Roster.Web.Security;

namespace Roster.Web.Endpoints;

/// <summary>
/// Dashboard actions as plain form posts, so they work without the interactive (real-time) connection:
/// duplicate a game into a new edition and delete a game. Only the organizer who owns the game can do either.
/// </summary>
public static class GameActionEndpoints
{
    public static IEndpointRouteBuilder MapGameActionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/organizer/actions/games/{gameId:guid}").WithTags("Game actions").RequireAuthorization();

        group.MapPost("/duplicate", async (
            Guid gameId,
            HttpContext context,
            GameDuplicationService service,
            IGameRepository games,
            IStringLocalizer<SharedResource> localizer,
            CancellationToken cancellationToken) =>
        {
            var organizerId = OrganizerIdentity.GetId(context.User);
            var source = organizerId is null ? null : await games.GetByIdAsync(gameId, cancellationToken);
            if (organizerId is null || source is null || !source.IsOwnedBy(organizerId))
            {
                return Results.Redirect("/organizer?error=duplicate");
            }

            var newId = await service.DuplicateAsync(gameId, organizerId, string.Format(System.Globalization.CultureInfo.CurrentCulture, localizer["DuplicateNameFormat"].Value, source.Name), cancellationToken);
            return Results.Redirect(newId is null ? "/organizer?error=duplicate" : $"/organizer/games/{newId}");
        })
        .WithName("DuplicateGame");

        group.MapPost("/delete", async (
            Guid gameId,
            HttpContext context,
            GameLifecycleService lifecycle,
            IGameRepository games,
            CancellationToken cancellationToken) =>
        {
            var organizerId = OrganizerIdentity.GetId(context.User);
            var game = organizerId is null ? null : await games.GetByIdAsync(gameId, cancellationToken);
            if (organizerId is null || game is null || !game.IsOwnedBy(organizerId))
            {
                return Results.Redirect("/organizer?error=delete");
            }

            await lifecycle.DeleteGameAsync(gameId, cancellationToken);
            return Results.Redirect("/organizer?deleted=1");
        })
        .WithName("DeleteGame");

        return app;
    }
}
