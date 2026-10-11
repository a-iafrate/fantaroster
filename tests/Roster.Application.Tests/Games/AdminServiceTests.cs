using NSubstitute;
using Roster.Application.Admin;
using Roster.Application.Plans;
using Roster.Application.Ports.Data;
using Roster.Application.Services;
using Roster.Domain.Games;
using Shouldly;
using Xunit;

namespace Roster.Application.Tests.Games;

public class AdminServiceTests
{
    private readonly IAdminQueries _queries = Substitute.For<IAdminQueries>();
    private readonly IGameRepository _games = Substitute.For<IGameRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly AdminService _service;

    public AdminServiceTests()
    {
        _service = new AdminService(_queries, _games, _unitOfWork, new PlanLimits(50, 300), TimeProvider.System);
    }

    [Fact]
    public async Task GetGamesAsync_AppliesThePlanLimitsToEachGame()
    {
        var plain = new AdminGameRow(Guid.NewGuid(), "Plain", "a@example.test", false, GameState.Open, 3, DateTimeOffset.UnixEpoch, null, null);
        var community = plain with { Id = Guid.NewGuid(), Name = "Community", OrganizerIsCommunity = true };
        var launch = plain with { Id = Guid.NewGuid(), Name = "Launch", LimitOverride = 1000, LimitLabel = "Launch event" };
        _queries.GetGamesAsync(Arg.Any<CancellationToken>()).Returns([plain, community, launch]);

        var games = await _service.GetGamesAsync();

        games.Select(g => g.EffectiveLimit).ShouldBe([50, 300, 1000]);
        games[2].LimitLabel.ShouldBe("Launch event");
    }

    [Fact]
    public async Task SetGameLimitAsync_SavesTheOverride()
    {
        var game = new Game(Guid.NewGuid(), "slug", "Game", "pack", "brand", "en", 3, true, 2m, "CODE1", DateTimeOffset.UnixEpoch);
        _games.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(game);

        (await _service.SetGameLimitAsync(game.Id, 800, "Launch event")).ShouldBeTrue();

        game.ParticipantLimitOverride.ShouldBe(800);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetGameLimitAsync_UnknownGame_ReturnsFalse() =>
        (await _service.SetGameLimitAsync(Guid.NewGuid(), 800, null)).ShouldBeFalse();
}
