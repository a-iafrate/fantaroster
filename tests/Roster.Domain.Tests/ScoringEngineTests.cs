using System;
using System.Collections.Generic;
using Roster.Domain.Games;
using Roster.Domain.Participants;
using Roster.Domain.Scores;
using Roster.Domain.Scoring;
using Shouldly;
using Xunit;

namespace Roster.Domain.Tests;

public class ScoringEngineTests
{
    private Game CreateGame(bool captainEnabled, decimal captainMultiplier)
    {
        return new Game(Guid.NewGuid(), "test", "Test", "generic", "fanta", "en", 3, captainEnabled, captainMultiplier, "CODE", DateTimeOffset.UtcNow);
    }

    [Fact]
    public void CalculateLeaderboard_Ties_ShouldUseStandardCompetitionRanking()
    {
        var game = CreateGame(false, 1.0m);
        var el1 = Guid.NewGuid();
        var el2 = Guid.NewGuid();
        var el3 = Guid.NewGuid();

        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var p3 = Guid.NewGuid();
        var p4 = Guid.NewGuid();

        var lineups = new List<Lineup>
        {
            new Lineup(p1, new[] { el1 }, null, DateTimeOffset.UtcNow),
            new Lineup(p2, new[] { el1 }, null, DateTimeOffset.UtcNow),
            new Lineup(p3, new[] { el2 }, null, DateTimeOffset.UtcNow),
            new Lineup(p4, new[] { el3 }, null, DateTimeOffset.UtcNow)
        };

        var ruleId = Guid.NewGuid();
        var entries = new List<ScoreEntry>
        {
            new ScoreEntry(Guid.NewGuid(), game.Id, el1, null, ruleId, 10, ScoreSource.Referee, "ref", DateTimeOffset.UtcNow, "k1"),
            new ScoreEntry(Guid.NewGuid(), game.Id, el2, null, ruleId, 5, ScoreSource.Referee, "ref", DateTimeOffset.UtcNow, "k2"),
            new ScoreEntry(Guid.NewGuid(), game.Id, el3, null, ruleId, 2, ScoreSource.Referee, "ref", DateTimeOffset.UtcNow, "k3")
        };

        var leaderboard = ScoringEngine.CalculateLeaderboard(game, entries, lineups);

        leaderboard.Participants.Count.ShouldBe(4);
        leaderboard.Participants[0].Score.ShouldBe(10);
        leaderboard.Participants[0].Rank.ShouldBe(1);
        leaderboard.Participants[1].Score.ShouldBe(10);
        leaderboard.Participants[1].Rank.ShouldBe(1);
        leaderboard.Participants[2].Score.ShouldBe(5);
        leaderboard.Participants[2].Rank.ShouldBe(3);
        leaderboard.Participants[3].Score.ShouldBe(2);
        leaderboard.Participants[3].Rank.ShouldBe(4);
    }

    [Fact]
    public void CalculateLeaderboard_WithCaptainMultiplier_ShouldMultiplyCorrectly()
    {
        var game = CreateGame(true, 2.0m);
        var el1 = Guid.NewGuid();
        var el2 = Guid.NewGuid();
        var p1 = Guid.NewGuid();

        var lineups = new List<Lineup>
        {
            new Lineup(p1, new[] { el1, el2 }, el1, DateTimeOffset.UtcNow)
        };

        var ruleId = Guid.NewGuid();
        var entries = new List<ScoreEntry>
        {
            new ScoreEntry(Guid.NewGuid(), game.Id, el1, null, ruleId, 10, ScoreSource.Referee, "ref", DateTimeOffset.UtcNow, "k1"),
            new ScoreEntry(Guid.NewGuid(), game.Id, el2, null, ruleId, 5, ScoreSource.Referee, "ref", DateTimeOffset.UtcNow, "k2")
        };

        var leaderboard = ScoringEngine.CalculateLeaderboard(game, entries, lineups);

        leaderboard.Participants.Count.ShouldBe(1);
        leaderboard.Participants[0].Score.ShouldBe(25);
    }

    [Fact]
    public void CalculateLeaderboard_VoidedEntries_ShouldBeIgnored()
    {
        var game = CreateGame(false, 1.0m);
        var el1 = Guid.NewGuid();
        var p1 = Guid.NewGuid();

        var lineups = new List<Lineup> { new Lineup(p1, new[] { el1 }, null, DateTimeOffset.UtcNow) };

        var ruleId = Guid.NewGuid();
        var entry1 = new ScoreEntry(Guid.NewGuid(), game.Id, el1, null, ruleId, 10, ScoreSource.Referee, "ref", DateTimeOffset.UtcNow, "k1");
        var entry2 = new ScoreEntry(Guid.NewGuid(), game.Id, el1, null, ruleId, 5, ScoreSource.Referee, "ref", DateTimeOffset.UtcNow, "k2");

        entry2.Void("ref2", DateTimeOffset.UtcNow);

        var leaderboard = ScoringEngine.CalculateLeaderboard(game, new[] { entry1, entry2 }, lineups);

        leaderboard.Participants[0].Score.ShouldBe(10);
        leaderboard.Elements[0].Score.ShouldBe(10);
    }

    [Fact]
    public void CalculateLeaderboard_ParticipantPersonalEntries_ShouldBeAdded()
    {
        var game = CreateGame(false, 1.0m);
        var p1 = Guid.NewGuid();

        var lineups = new List<Lineup> { new Lineup(p1, Array.Empty<Guid>(), null, DateTimeOffset.UtcNow) };

        var ruleId = Guid.NewGuid();
        var entry = new ScoreEntry(Guid.NewGuid(), game.Id, null, p1, ruleId, 15, ScoreSource.Sponsor, "sponsor", DateTimeOffset.UtcNow, "k1");

        var leaderboard = ScoringEngine.CalculateLeaderboard(game, new[] { entry }, lineups);

        leaderboard.Participants[0].Score.ShouldBe(15);
    }
}
