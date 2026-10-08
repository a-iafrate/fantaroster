using System;
using Roster.Domain.Games;
using Shouldly;
using Xunit;

namespace Roster.Domain.Tests;

public class GameTests
{
    private Game CreateTestGame() => new Game(Guid.NewGuid(), "test-game", "Test Game", "generic", "FantaRoster", "it-IT", 3, true, 2.0m, "ABC12", DateTimeOffset.UtcNow);

    [Fact]
    public void Game_New_ShouldStartInDraft()
    {
        var game = CreateTestGame();
        game.State.ShouldBe(GameState.Draft);
    }

    [Fact]
    public void Open_FromDraft_ShouldTransitionToOpen()
    {
        var game = CreateTestGame();
        game.Open();
        game.State.ShouldBe(GameState.Open);
    }

    [Fact]
    public void Open_FromLive_ShouldThrow()
    {
        var game = CreateTestGame();
        game.Open();
        game.GoLive(DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(() => game.Open());
    }
}
