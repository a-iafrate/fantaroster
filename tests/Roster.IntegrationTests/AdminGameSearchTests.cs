using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Roster.Application.Admin;
using Roster.Application.Ports.Data;
using Roster.Domain.Games;
using Roster.Domain.Participants;
using Roster.Infrastructure.Data;
using Shouldly;
using Xunit;

namespace Roster.IntegrationTests;

/// <summary>The administration list of games: search, filters, order and paging are done by the database.</summary>
public sealed class AdminGameSearchTests : IClassFixture<RosterWebFactory>
{
    private readonly RosterWebFactory _factory;

    public AdminGameSearchTests(RosterWebFactory factory)
    {
        _factory = factory;
    }

    private sealed record Seeded(string Tag, string OrganizerEmail, Guid Big, Guid Small, Guid Custom);

    private static Game NewGame(string name, DateTimeOffset createdAt)
    {
        var id = Guid.NewGuid();
        return new Game(id, $"slug-{id:N}", name, "pack", "brand", "en", 3, true, 2m, id.ToString("N")[..8].ToUpperInvariant(), createdAt);
    }

    /// <summary>Three games of one organizer, with names that carry a tag no other test uses.</summary>
    private async Task<Seeded> SeedAsync(IServiceProvider services)
    {
        var tag = "zq" + Guid.NewGuid().ToString("N")[..8];
        var users = services.GetRequiredService<UserManager<OrganizerUser>>();
        var db = services.GetRequiredService<RosterDbContext>();
        var email = $"{tag}@example.test";
        var user = new OrganizerUser { UserName = email, Email = email, DisplayName = tag };
        (await users.CreateAsync(user)).Succeeded.ShouldBeTrue();

        var now = DateTimeOffset.UtcNow;
        var big = NewGame($"{tag} big", now.AddMinutes(-3));
        var small = NewGame($"{tag} small", now.AddMinutes(-2));
        var custom = NewGame($"{tag} custom", now.AddMinutes(-1));
        foreach (var game in new[] { big, small, custom })
        {
            game.AssignToOrganizer(user.Id);
            game.Open();
        }

        custom.SetParticipantLimitOverride(500, "Launch event");
        small.GoLive(now);
        db.Games.AddRange(big, small, custom);
        for (var i = 0; i < 3; i++)
        {
            db.Participants.Add(new Participant(Guid.NewGuid(), big.Id, $"p{i}", $"hash-{Guid.NewGuid():N}", now));
        }

        db.Participants.Add(new Participant(Guid.NewGuid(), small.Id, "only", $"hash-{Guid.NewGuid():N}", now));
        await db.SaveChangesAsync();
        return new Seeded(tag, email, big.Id, small.Id, custom.Id);
    }

    [Fact]
    public async Task Search_FindsGamesByNameAndByOrganizerEmail()
    {
        using var scope = _factory.Services.CreateScope();
        var seeded = await SeedAsync(scope.ServiceProvider);
        var queries = scope.ServiceProvider.GetRequiredService<IAdminQueries>();

        var byName = await queries.SearchGamesAsync(new AdminGameQuery(Search: $"{seeded.Tag} sma"));
        var byEmail = await queries.SearchGamesAsync(new AdminGameQuery(Search: seeded.OrganizerEmail));

        byName.Total.ShouldBe(1);
        byName.Rows.Single().Id.ShouldBe(seeded.Small);
        byName.Rows.Single().OrganizerEmail.ShouldBe(seeded.OrganizerEmail);
        byEmail.Total.ShouldBe(3);
    }

    [Fact]
    public async Task Search_FiltersByStateAndByCustomLimit()
    {
        using var scope = _factory.Services.CreateScope();
        var seeded = await SeedAsync(scope.ServiceProvider);
        var queries = scope.ServiceProvider.GetRequiredService<IAdminQueries>();

        var live = await queries.SearchGamesAsync(new AdminGameQuery(Search: seeded.Tag, State: GameState.Live));
        var custom = await queries.SearchGamesAsync(new AdminGameQuery(Search: seeded.Tag, CustomLimitOnly: true));

        live.Rows.Select(r => r.Id).ShouldBe([seeded.Small]);
        custom.Rows.Select(r => r.Id).ShouldBe([seeded.Custom]);
        custom.Rows.Single().LimitOverride.ShouldBe(500);
        custom.Rows.Single().LimitLabel.ShouldBe("Launch event");
    }

    [Fact]
    public async Task Search_SortsByPlayersNameAndDateAndCountsPlayers()
    {
        using var scope = _factory.Services.CreateScope();
        var seeded = await SeedAsync(scope.ServiceProvider);
        var queries = scope.ServiceProvider.GetRequiredService<IAdminQueries>();

        var byPlayers = await queries.SearchGamesAsync(new AdminGameQuery(Search: seeded.Tag, Sort: AdminGameSort.MostPlayers));
        var byName = await queries.SearchGamesAsync(new AdminGameQuery(Search: seeded.Tag, Sort: AdminGameSort.Name));
        var newest = await queries.SearchGamesAsync(new AdminGameQuery(Search: seeded.Tag));
        var oldest = await queries.SearchGamesAsync(new AdminGameQuery(Search: seeded.Tag, Sort: AdminGameSort.Oldest));

        byPlayers.Rows.Select(r => r.Id).ShouldBe([seeded.Big, seeded.Small, seeded.Custom]);
        byPlayers.Rows.Select(r => r.Participants).ShouldBe([3, 1, 0]);
        byName.Rows.Select(r => r.Id).ShouldBe([seeded.Big, seeded.Custom, seeded.Small]);
        newest.Rows.Select(r => r.Id).ShouldBe([seeded.Custom, seeded.Small, seeded.Big]);
        oldest.Rows.Select(r => r.Id).ShouldBe([seeded.Big, seeded.Small, seeded.Custom]);
    }

    [Fact]
    public async Task Search_PagesTheResultsAndReportsTheTotal()
    {
        using var scope = _factory.Services.CreateScope();
        var seeded = await SeedAsync(scope.ServiceProvider);
        var queries = scope.ServiceProvider.GetRequiredService<IAdminQueries>();

        var first = await queries.SearchGamesAsync(new AdminGameQuery(Search: seeded.Tag, Page: 1, PageSize: 2));
        var second = await queries.SearchGamesAsync(new AdminGameQuery(Search: seeded.Tag, Page: 2, PageSize: 2));

        first.Total.ShouldBe(3);
        first.Rows.Select(r => r.Id).ShouldBe([seeded.Custom, seeded.Small]);
        second.Total.ShouldBe(3);
        second.Rows.Select(r => r.Id).ShouldBe([seeded.Big]);
    }

    [Fact]
    public async Task Search_NothingMatches_ReturnsAnEmptyPage()
    {
        using var scope = _factory.Services.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<IAdminQueries>();

        var result = await queries.SearchGamesAsync(new AdminGameQuery(Search: "no-such-game-" + Guid.NewGuid().ToString("N")));

        result.Total.ShouldBe(0);
        result.Rows.ShouldBeEmpty();
    }
}
