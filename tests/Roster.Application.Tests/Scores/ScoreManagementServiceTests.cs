using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Roster.Application.Games.Queries;
using Roster.Application.Ports.Data;
using Roster.Application.Ports.Notifications;
using Roster.Application.Scores.Commands;
using Roster.Application.Services;
using Roster.Domain.Games;
using Roster.Domain.Scores;
using Shouldly;
using Xunit;

namespace Roster.Application.Tests.Scores;

public class ScoreManagementServiceTests
{
    private readonly IGameRepository _gameRepository = Substitute.For<IGameRepository>();
    private readonly IScoreEntryRepository _scoreEntryRepository = Substitute.For<IScoreEntryRepository>();
    private readonly IRuleRepository _ruleRepository = Substitute.For<IRuleRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IGameNotificationService _notificationService = Substitute.For<IGameNotificationService>();
    private readonly IParticipantRepository _participantRepository = Substitute.For<IParticipantRepository>();
    private readonly IElementRepository _elementRepository = Substitute.For<IElementRepository>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly LeaderboardStateStore _stateStore = new();

    private readonly ScoreManagementService _sut;

    public ScoreManagementServiceTests()
    {
        var leaderboardService = new LeaderboardService(
            _gameRepository,
            _scoreEntryRepository,
            _participantRepository,
            _elementRepository,
            _stateStore,
            _notificationService);

        _sut = new ScoreManagementService(
            _gameRepository,
            _scoreEntryRepository,
            _ruleRepository,
            _unitOfWork,
            _notificationService,
            leaderboardService,
            _timeProvider);
    }

    [Fact]
    public async Task AssignScoreAsync_WhenGameNotLive_ThrowsInvalidOperationException()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = new Game(gameId, "slug", "Test Game", "generic", "brand", "en", 3, true, 2m, "CODE", DateTimeOffset.UnixEpoch); // Initially Draft
        _gameRepository.GetByIdAsync(gameId, Arg.Any<CancellationToken>()).Returns(game);

        var command = new AssignScoreCommand(gameId, Guid.NewGuid(), null, Guid.NewGuid(), "Referee1", "key1");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => _sut.AssignScoreAsync(command));
    }

    [Fact]
    public async Task AssignScoreAsync_WhenLiveGame_SavesScoreAndNotifies()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = new Game(gameId, "slug", "Test Game", "generic", "brand", "en", 3, true, 2m, "CODE", DateTimeOffset.UnixEpoch);
        game.Open();
        game.GoLive(DateTimeOffset.UtcNow); // State is now Live

        var ruleId = Guid.NewGuid();
        var rule = new Rule(ruleId, gameId, "Goal", 10, "Category", RuleTarget.Element, 0);

        _gameRepository.GetByIdAsync(gameId, Arg.Any<CancellationToken>()).Returns(game);
        _ruleRepository.GetByIdAsync(ruleId, Arg.Any<CancellationToken>()).Returns(rule);

        _elementRepository.GetByGameIdAsync(gameId, Arg.Any<CancellationToken>()).Returns(new System.Collections.Generic.List<Roster.Domain.Elements.Element>());
        _participantRepository.GetParticipantsByGameIdAsync(gameId, Arg.Any<CancellationToken>()).Returns(new System.Collections.Generic.List<Roster.Domain.Participants.Participant>());
        _participantRepository.GetLineupsByGameIdAsync(gameId, Arg.Any<CancellationToken>()).Returns(new System.Collections.Generic.List<Roster.Domain.Participants.Lineup>());
        _scoreEntryRepository.GetValidEntriesByGameIdAsync(gameId, Arg.Any<CancellationToken>()).Returns(new System.Collections.Generic.List<ScoreEntry>());

        var command = new AssignScoreCommand(gameId, Guid.NewGuid(), null, ruleId, "Referee1", "key1");

        // Act
        var resultId = await _sut.AssignScoreAsync(command);

        // Assert
        resultId.ShouldNotBeNull();
        _scoreEntryRepository.Received(1).Add(Arg.Is<ScoreEntry>(e => e.RuleId == ruleId));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _notificationService.Received(1).NotifyScoreEntryAddedAsync(gameId, resultId.Value, Arg.Any<CancellationToken>());
        await _notificationService.Received(1).NotifyLeaderboardUpdatedAsync(gameId, Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssignScoreAsync_WhenIdempotencyKeyExists_ReturnsNull()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = new Game(gameId, "slug", "Test Game", "generic", "brand", "en", 3, true, 2m, "CODE", DateTimeOffset.UnixEpoch);
        game.Open();
        game.GoLive(DateTimeOffset.UtcNow);
        _gameRepository.GetByIdAsync(gameId, Arg.Any<CancellationToken>()).Returns(game);

        _scoreEntryRepository.ExistsByIdempotencyKeyAsync(gameId, "key1", Arg.Any<CancellationToken>()).Returns(true);

        var command = new AssignScoreCommand(gameId, Guid.NewGuid(), null, Guid.NewGuid(), "Referee1", "key1");

        // Act
        var resultId = await _sut.AssignScoreAsync(command);

        // Assert
        resultId.ShouldBeNull();
        _scoreEntryRepository.DidNotReceive().Add(Arg.Any<ScoreEntry>());
    }
}
