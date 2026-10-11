using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Roster.Domain.Elements;
using Roster.Domain.Games;
using Roster.Domain.Participants;
using Roster.Infrastructure.Data;
using Roster.Infrastructure.Security;
using Shouldly;
using Xunit;

namespace Roster.IntegrationTests;

public sealed class OrganizerAccountTests : IClassFixture<RosterWebFactory>
{
    private readonly RosterWebFactory _factory;

    public OrganizerAccountTests(RosterWebFactory factory)
    {
        _factory = factory;
    }

    private async Task<(string Id, Guid GameId, Guid OtherGameId)> SeedAsync(IServiceProvider services)
    {
        var users = services.GetRequiredService<UserManager<OrganizerUser>>();
        var db = services.GetRequiredService<RosterDbContext>();

        var email = $"account-{Guid.NewGuid():N}@example.test";
        var user = new OrganizerUser { UserName = email, Email = email, DisplayName = "Account Test" };
        (await users.CreateAsync(user)).Succeeded.ShouldBeTrue();

        var game = NewGame("Mine");
        game.AssignToOrganizer(user.Id);
        var other = NewGame("Someone else's");
        other.AssignToOrganizer("another-organizer");
        db.Games.AddRange(game, other);
        db.Elements.Add(new Element(Guid.NewGuid(), game.Id, null, null, "Red Lions", "Bologna", null, "Group A", ConsentStatus.NotRequired));
        db.Participants.Add(new Participant(Guid.NewGuid(), game.Id, "LaPrimaFila", "secret-hash", DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();

        return (user.Id, game.Id, other.Id);
    }

    private static Game NewGame(string name)
    {
        var id = Guid.NewGuid();
        return new Game(id, $"slug-{id:N}", name, "pack", "brand", "en", 3, true, 2m, id.ToString("N")[..8].ToUpperInvariant(), DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Export_contains_only_the_data_of_the_organizer_and_no_secrets()
    {
        using var scope = _factory.Services.CreateScope();
        var (organizerId, _, _) = await SeedAsync(scope.ServiceProvider);
        var service = scope.ServiceProvider.GetRequiredService<OrganizerAccountService>();

        var export = await service.ExportAsync(organizerId, TimeProvider.System);

        export.ShouldNotBeNull();
        export.Games.Select(g => g.Name).ShouldBe(["Mine"]);
        export.Games[0].Elements.Select(e => e.Name).ShouldBe(["Red Lions"]);
        export.Games[0].Participants.Select(p => p.Nickname).ShouldBe(["LaPrimaFila"]);
        System.Text.Json.JsonSerializer.Serialize(export).ShouldNotContain("secret-hash");
    }

    [Fact]
    public async Task Deleting_the_account_removes_the_user_and_their_games_but_not_other_games()
    {
        using var scope = _factory.Services.CreateScope();
        var (organizerId, gameId, otherGameId) = await SeedAsync(scope.ServiceProvider);
        var service = scope.ServiceProvider.GetRequiredService<OrganizerAccountService>();
        var db = scope.ServiceProvider.GetRequiredService<RosterDbContext>();

        (await service.DeleteAccountAsync(organizerId)).ShouldBeTrue();

        (await scope.ServiceProvider.GetRequiredService<UserManager<OrganizerUser>>().FindByIdAsync(organizerId)).ShouldBeNull();
        (await db.Games.AnyAsync(g => g.Id == gameId)).ShouldBeFalse();
        (await db.Elements.AnyAsync(e => e.GameId == gameId)).ShouldBeFalse();
        (await db.Participants.AnyAsync(p => p.GameId == gameId)).ShouldBeFalse();
        (await db.Games.AnyAsync(g => g.Id == otherGameId)).ShouldBeTrue();
    }

    [Fact]
    public async Task Export_and_delete_of_an_unknown_organizer_do_nothing()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<OrganizerAccountService>();

        (await service.ExportAsync("nobody", TimeProvider.System)).ShouldBeNull();
        (await service.DeleteAccountAsync("nobody")).ShouldBeFalse();
    }
}
