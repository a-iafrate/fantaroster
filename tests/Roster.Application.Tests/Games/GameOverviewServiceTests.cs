using NSubstitute;
using Roster.Application.Ports.Data;
using Roster.Application.Services;
using Roster.Domain.Elements;
using Roster.Domain.Games;
using Roster.Domain.Participants;
using Roster.Domain.Referees;
using Roster.Domain.Scores;
using Shouldly;
using Xunit;

namespace Roster.Application.Tests.Games;

public sealed class GameOverviewServiceTests
{
    private readonly IParticipantRepository _participants = Substitute.For<IParticipantRepository>();
    private readonly IElementRepository _elements = Substitute.For<IElementRepository>();
    private readonly IRuleRepository _rules = Substitute.For<IRuleRepository>();
    private readonly IRefereeRepository _referees = Substitute.For<IRefereeRepository>();
    private readonly IScoreEntryRepository _scoreEntries = Substitute.For<IScoreEntryRepository>();
    private readonly GameOverviewService _sut;
    private readonly Guid _gameId = Guid.NewGuid();

    public GameOverviewServiceTests()
    {
        var feed = new ActivityFeedService(_scoreEntries, _elements, _participants, _rules);
        _sut = new GameOverviewService(_participants, _elements, _rules, _referees, _scoreEntries, feed);
    }

    private Element ElementWith(ConsentStatus status) =>
        new(Guid.NewGuid(), _gameId, null, null, "Element", null, null, null, status);

    [Fact]
    public async Task GetAsync_counts_consents_only_for_elements_that_need_them()
    {
        _elements.GetByGameIdAsync(_gameId, Arg.Any<CancellationToken>()).Returns(new List<Element>
        {
            ElementWith(ConsentStatus.NotRequired),
            ElementWith(ConsentStatus.Accepted),
            ElementWith(ConsentStatus.Accepted),
            ElementWith(ConsentStatus.Pending),
            ElementWith(ConsentStatus.Declined),
        });
        _participants.GetParticipantsByGameIdAsync(_gameId, Arg.Any<CancellationToken>()).Returns(new List<Participant>());
        _rules.GetByGameIdAsync(_gameId, Arg.Any<CancellationToken>()).Returns(new List<Rule>());
        _referees.GetByGameIdAsync(_gameId, Arg.Any<CancellationToken>()).Returns(new List<Referee>());
        _scoreEntries.GetValidEntriesByGameIdAsync(_gameId, Arg.Any<CancellationToken>()).Returns(new List<ScoreEntry>());
        _scoreEntries.GetRecentEntriesByGameIdAsync(_gameId, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new List<ScoreEntry>());

        var result = await _sut.GetAsync(_gameId);

        result.Elements.ShouldBe(5);
        result.ConsentRequired.ShouldBe(4);
        result.ConsentAccepted.ShouldBe(2);
        result.ConsentPending.ShouldBe(1);
        result.ConsentDeclined.ShouldBe(1);
        result.LatestPoints.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAsync_counts_participants_rules_referees_and_valid_points()
    {
        var entry = new ScoreEntry(Guid.NewGuid(), _gameId, Guid.NewGuid(), null, Guid.NewGuid(), 5, ScoreSource.Referee, "Ref", DateTimeOffset.UtcNow, "k1");
        _elements.GetByGameIdAsync(_gameId, Arg.Any<CancellationToken>()).Returns(new List<Element>());
        _participants.GetParticipantsByGameIdAsync(_gameId, Arg.Any<CancellationToken>()).Returns(
            new List<Participant> { new(Guid.NewGuid(), _gameId, "A", "h1", DateTimeOffset.UtcNow), new(Guid.NewGuid(), _gameId, "B", "h2", DateTimeOffset.UtcNow) });
        _rules.GetByGameIdAsync(_gameId, Arg.Any<CancellationToken>()).Returns(new List<Rule> { new(Guid.NewGuid(), _gameId, "Rule", 5, "c", RuleTarget.Element) });
        _referees.GetByGameIdAsync(_gameId, Arg.Any<CancellationToken>()).Returns(new List<Referee> { new(Guid.NewGuid(), _gameId, "R", "hash", []) });
        _scoreEntries.GetValidEntriesByGameIdAsync(_gameId, Arg.Any<CancellationToken>()).Returns(new List<ScoreEntry> { entry });
        _scoreEntries.GetRecentEntriesByGameIdAsync(_gameId, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new List<ScoreEntry> { entry });

        var result = await _sut.GetAsync(_gameId);

        result.Participants.ShouldBe(2);
        result.Rules.ShouldBe(1);
        result.Referees.ShouldBe(1);
        result.PointsAssigned.ShouldBe(1);
        result.LatestPoints.Count.ShouldBe(1);
    }
}
