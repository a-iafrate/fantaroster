using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Roster.Application.Games.Queries;
using Roster.Application.Ports.Data;
using Roster.Application.Services;
using Roster.Domain.Elements;
using Roster.Domain.Games;
using Roster.Domain.Participants;
using Roster.Domain.Scores;
using Shouldly;
using Xunit;

namespace Roster.Application.Tests.Games.Queries;

public class LeaderboardServiceTests
{
    private readonly IGameRepository _gameRepository = Substitute.For<IGameRepository>();
    private readonly IScoreEntryRepository _scoreEntryRepository = Substitute.For<IScoreEntryRepository>();
    private readonly IParticipantRepository _participantRepository = Substitute.For<IParticipantRepository>();
    private readonly IElementRepository _elementRepository = Substitute.For<IElementRepository>();
    private readonly Roster.Application.Ports.Notifications.IGameNotificationService _notificationService = Substitute.For<Roster.Application.Ports.Notifications.IGameNotificationService>();
    private readonly LeaderboardService _sut;

    public LeaderboardServiceTests()
    {
        var stateStore = new LeaderboardStateStore();
        _sut = new LeaderboardService(
            _gameRepository,
            _scoreEntryRepository,
            _participantRepository,
            _elementRepository,
            stateStore,
            _notificationService);
    }

    [Fact]
    public async Task HandleAsync_WhenCalled_ReturnsLeaderboard()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var query = new GetLeaderboardQuery(gameId, callerId);

        var game = new Game(gameId, "test-game", "Test Game", "generic", "FantaRoster", "en", 3, true, 2.0m, "CODE", DateTimeOffset.UtcNow);
        var elementId = Guid.NewGuid();

        var lineup = new Lineup(callerId, new[] { elementId }, null, DateTimeOffset.UtcNow);

        var caller = new Participant(callerId, gameId, "Me", Guid.NewGuid().ToString(), DateTimeOffset.UtcNow);

        var scoreEntry = new ScoreEntry(Guid.NewGuid(), gameId, elementId, null, Guid.NewGuid(), 10, ScoreSource.Referee, "Test", DateTimeOffset.UtcNow, "Test", null);

        _gameRepository.GetByIdAsync(gameId, Arg.Any<CancellationToken>()).Returns(game);
        _scoreEntryRepository.GetValidEntriesByGameIdAsync(gameId, Arg.Any<CancellationToken>()).Returns(new List<ScoreEntry> { scoreEntry });
        _participantRepository.GetLineupsByGameIdAsync(gameId, Arg.Any<CancellationToken>()).Returns(new List<Lineup> { lineup });
        _participantRepository.GetParticipantsByGameIdAsync(gameId, Arg.Any<CancellationToken>()).Returns(new List<Participant> { caller });

        // Note: For elements, we need a mock Element object, but Element has no public constructor with all fields easily set without reflection if it's protected.
        // Actually, let's see how Element is created. We will just mock IElementRepository.

        // Assuming Element has a constructor or factory. We can just skip setting up elements if it returns empty, element score will have Name = "" or we can just mock it.
        // I will return an empty list for elements, which means ElementName might be null.
        _elementRepository.GetByGameIdAsync(gameId, Arg.Any<CancellationToken>()).Returns(new List<Element>());

        // Act
        var result = await _sut.HandleAsync(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.MyPosition.ShouldNotBeNull();
        result.MyPosition.Nickname.ShouldBe("Me");
        result.MyPosition.TotalScore.ShouldBe(10);
        result.TopPositions.Count.ShouldBe(1);
    }
}
