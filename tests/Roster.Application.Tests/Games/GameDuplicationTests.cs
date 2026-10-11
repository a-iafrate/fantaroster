using NSubstitute;
using Roster.Application.Ports.Data;
using Roster.Application.Services;
using Roster.Domain.Elements;
using Roster.Domain.Games;
using Roster.Domain.Sources;
using Shouldly;
using Xunit;

namespace Roster.Application.Tests.Games;

public class GameDuplicationTests
{
    private readonly IGameRepository _games = Substitute.For<IGameRepository>();
    private readonly IRuleRepository _rules = Substitute.For<IRuleRepository>();
    private readonly IElementRepository _elements = Substitute.For<IElementRepository>();
    private readonly ISourceBindingRepository _bindings = Substitute.For<ISourceBindingRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly GameDuplicationService _service;
    private readonly Game _source;

    public GameDuplicationTests()
    {
        _service = new GameDuplicationService(_games, _rules, _elements, _bindings, _unitOfWork, TimeProvider.System);
        _source = new Game(Guid.NewGuid(), "slug", "Edition 1", "pack", "brand", "en", 4, true, 3m, "CODE1", DateTimeOffset.UnixEpoch);
        _source.AssignToOrganizer("organizer-1");
        _games.GetByIdAsync(_source.Id, Arg.Any<CancellationToken>()).Returns(_source);
        _rules.GetByGameIdAsync(_source.Id, Arg.Any<CancellationToken>()).Returns([new Rule(Guid.NewGuid(), _source.Id, "Goal", 3, "General", RuleTarget.Element, 1)]);
        _bindings.GetByGameIdAsync(_source.Id, Arg.Any<CancellationToken>()).Returns([]);
    }

    [Fact]
    public async Task DuplicateAsync_CopiesSettingsRulesAndElementsAndResetsConsent()
    {
        var accepted = new Element(Guid.NewGuid(), _source.Id, null, "ext-1", "Red Lions", null, null, "A", ConsentStatus.Accepted);
        var open = new Element(Guid.NewGuid(), _source.Id, null, "ext-2", "Blue Sharks", null, null, "B", ConsentStatus.NotRequired);
        var missing = new Element(Guid.NewGuid(), _source.Id, null, "ext-3", "Gone", null, null, null, ConsentStatus.NotRequired);
        missing.MarkMissingFromSource();
        _elements.GetByGameIdAsync(_source.Id, Arg.Any<CancellationToken>()).Returns([accepted, open, missing]);
        var added = new List<Element>();
        _elements.When(e => e.Add(Arg.Any<Element>())).Do(c => added.Add(c.Arg<Element>()));
        Game? copy = null;
        _games.When(g => g.Add(Arg.Any<Game>())).Do(c => copy = c.Arg<Game>());

        var id = await _service.DuplicateAsync(_source.Id, "organizer-1", "Edition 2");

        id.ShouldNotBeNull();
        copy.ShouldNotBeNull();
        copy.Id.ShouldBe(id.Value);
        copy.Name.ShouldBe("Edition 2");
        copy.LineupSize.ShouldBe(4);
        copy.CaptainMultiplier.ShouldBe(3m);
        copy.State.ShouldBe(GameState.Draft);
        copy.JoinCode.ShouldNotBe(_source.JoinCode);
        copy.IsOwnedBy("organizer-1").ShouldBeTrue();
        _rules.Received(1).Add(Arg.Is<Rule>(r => r.GameId == id.Value && r.Label == "Goal" && r.Points == 3));
        added.Select(e => e.Name).ShouldBe(["Red Lions", "Blue Sharks"]);
        added.Single(e => e.Name == "Red Lions").ConsentStatus.ShouldBe(ConsentStatus.Pending);
        added.Single(e => e.Name == "Blue Sharks").ConsentStatus.ShouldBe(ConsentStatus.NotRequired);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DuplicateAsync_GameOfAnotherOrganizer_ReturnsNullAndChangesNothing()
    {
        var id = await _service.DuplicateAsync(_source.Id, "someone-else", "Copy");

        id.ShouldBeNull();
        _games.DidNotReceive().Add(Arg.Any<Game>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DuplicateAsync_UnknownGame_ReturnsNull() =>
        (await _service.DuplicateAsync(Guid.NewGuid(), "organizer-1", "Copy")).ShouldBeNull();
}
