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

    [Fact]
    public void IsOwnedBy_GameWithoutOwner_BelongsToNobody()
    {
        var game = CreateTestGame();

        game.IsOwnedBy("organizer-1").ShouldBeFalse();
        game.IsOwnedBy(null).ShouldBeFalse();
    }

    [Fact]
    public void AssignToOrganizer_SetsTheOwner()
    {
        var game = CreateTestGame();

        game.AssignToOrganizer("organizer-1");

        game.IsOwnedBy("organizer-1").ShouldBeTrue();
        game.IsOwnedBy("organizer-2").ShouldBeFalse();
        game.IsOwnedBy(string.Empty).ShouldBeFalse();
    }

    [Fact]
    public void AssignToOrganizer_SameOrganizerTwice_IsAllowed()
    {
        var game = CreateTestGame();
        game.AssignToOrganizer("organizer-1");

        Should.NotThrow(() => game.AssignToOrganizer("organizer-1"));
    }

    [Fact]
    public void AssignToOrganizer_AnotherOrganizer_ShouldThrow()
    {
        var game = CreateTestGame();
        game.AssignToOrganizer("organizer-1");

        Should.Throw<InvalidOperationException>(() => game.AssignToOrganizer("organizer-2"));
    }

    [Fact]
    public void AssignToOrganizer_BlankId_ShouldThrow()
    {
        var game = CreateTestGame();

        Should.Throw<ArgumentException>(() => game.AssignToOrganizer(" "));
    }
}
