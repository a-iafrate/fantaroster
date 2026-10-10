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

public class ActivityFeedServiceTests
{
    private readonly IScoreEntryRepository _scoreEntryRepository = Substitute.For<IScoreEntryRepository>();
    private readonly IElementRepository _elementRepository = Substitute.For<IElementRepository>();
    private readonly IParticipantRepository _participantRepository = Substitute.For<IParticipantRepository>();
    private readonly IRuleRepository _ruleRepository = Substitute.For<IRuleRepository>();
    private readonly ActivityFeedService _sut;

    public ActivityFeedServiceTests()
    {
        _sut = new ActivityFeedService(
            _scoreEntryRepository,
            _elementRepository,
            _participantRepository,
            _ruleRepository);
    }

    [Fact]
    public async Task HandleAsync_WhenCalled_ReturnsActivityFeed()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var query = new GetActivityFeedQuery(gameId);

        var elementId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();
        var scoreEntry = new ScoreEntry(Guid.NewGuid(), gameId, elementId, null, ruleId, 10, ScoreSource.Referee, "Test", DateTimeOffset.UtcNow, "Test");

        _scoreEntryRepository.GetRecentEntriesByGameIdAsync(gameId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ScoreEntry> { scoreEntry });

        _elementRepository.GetByGameIdAsync(gameId, Arg.Any<CancellationToken>()).Returns(new List<Element>());
        _participantRepository.GetParticipantsByGameIdAsync(gameId, Arg.Any<CancellationToken>()).Returns(new List<Participant>());

        var rule = new Rule(ruleId, gameId, "Cool Rule", 10, "TestCategory", RuleTarget.Element);
        _ruleRepository.GetByGameIdAsync(gameId, Arg.Any<CancellationToken>()).Returns(new List<Rule> { rule });

        // Act
        var result = await _sut.HandleAsync(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].Points.ShouldBe(10);
        result[0].RuleLabel.ShouldBe("Cool Rule");
        result[0].Status.ShouldBe(ScoreStatus.Valid.ToString());
        result[0].ElementId.ShouldBe(elementId);
        result[0].ParticipantId.ShouldBeNull();
    }
}
