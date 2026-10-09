#pragma warning disable CS0619
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using NSubstitute;
using Roster.Application.Ports.Data;
using Roster.Application.Services;
using Roster.Domain.Games;
using Roster.Web;
using Roster.Web.Components.Pages.Organizer;
using Shouldly;
using Xunit;

namespace Roster.Web.Tests.Components;

public class RuleEditorTests : TestContext
{
    private readonly IRuleRepository _ruleRepository;
    private readonly IGameRepository _gameRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public RuleEditorTests()
    {
        _ruleRepository = Substitute.For<IRuleRepository>();
        _gameRepository = Substitute.For<IGameRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        var ruleService = new RuleManagementService(_ruleRepository, _gameRepository, _unitOfWork);
        Services.AddSingleton(ruleService);

        _localizer = Substitute.For<IStringLocalizer<SharedResource>>();
        _localizer[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.Arg<string>(), callInfo.Arg<string>()));
        Services.AddSingleton(_localizer);
    }

    [Fact]
    public void RendersRulesList_WhenRulesExist()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var rules = new List<Rule>
        {
            new Rule(Guid.NewGuid(), gameId, "Rule 1", 10, "Cat 1", RuleTarget.Element, 0),
            new Rule(Guid.NewGuid(), gameId, "Rule 2", -5, "Cat 2", RuleTarget.Element, 1)
        };
        _ruleRepository.GetByGameIdAsync(gameId, Arg.Any<CancellationToken>()).Returns(rules);

        // Act
        var cut = RenderComponent<RulesTab>(parameters => parameters
            .Add(p => p.GameId, gameId)
            .Add(p => p.GameState, GameState.Draft));

        // Assert
        cut.Markup.ShouldContain("Rule 1");
        cut.Markup.ShouldContain("Rule 2");
        cut.Markup.ShouldContain("+10");
        cut.Markup.ShouldContain("-5");
    }

    [Fact]
    public void CanOpenAddRuleModal()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        _ruleRepository.GetByGameIdAsync(gameId, Arg.Any<CancellationToken>()).Returns(new List<Rule>());

        var cut = RenderComponent<RulesTab>(parameters => parameters
            .Add(p => p.GameId, gameId)
            .Add(p => p.GameState, GameState.Draft));

        // Act
        cut.Find("button.mg-btn-primary").Click();

        // Assert
        cut.Markup.ShouldContain("AddRuleTitle");
        cut.Find("input[type='text']").ShouldNotBeNull(); // Label input
    }
}
