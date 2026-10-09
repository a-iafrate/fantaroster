#pragma warning disable CS0619
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using NSubstitute;
using Roster.Application.Ports;
using Roster.DomainPacks;
using Roster.Plugins.Abstractions;
using Roster.Web;
using Roster.Web.Components.Pages.Organizer;
using Roster.Web.Options;
using Shouldly;
using Xunit;

namespace Roster.Web.Tests.Components;

public class CreateGameWizardTests : TestContext
{
    private readonly IDomainPackLoader _packLoader;
    private readonly IGameCreationService _gameCreationService;
    private readonly IPluginRegistry _pluginRegistry;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CreateGameWizardTests()
    {
        _packLoader = Substitute.For<IDomainPackLoader>();
        _gameCreationService = Substitute.For<IGameCreationService>();
        _pluginRegistry = Substitute.For<IPluginRegistry>();
        
        Services.AddSingleton(_packLoader);
        Services.AddSingleton(_gameCreationService);
        Services.AddSingleton(_pluginRegistry);

        _localizer = Substitute.For<IStringLocalizer<SharedResource>>();
        _localizer[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.Arg<string>(), callInfo.Arg<string>()));
        Services.AddSingleton(_localizer);

        Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new BrandOptions { Id = "FantaRoster", Name = "FantaRoster" }));
    }

    [Fact]
    public void Wizard_StartsAtStep1_WithDomainPacks()
    {
        // Arrange
        _packLoader.GetAllPacks().Returns(new[] 
        {
            new DomainPack("generic", new Dictionary<string,string>{{"en", "Generic"}}, new Dictionary<string,string>{{"en", "Generic description"}}, false, Array.Empty<string>(), new Dictionary<string, PackTerminology>(), Array.Empty<PackRule>())
        });

        // Act
        var cut = RenderComponent<CreateGame>();

        // Assert
        cut.Markup.ShouldContain("CreateGameStep1Title");
        cut.Markup.ShouldContain("Generic description");
    }

    [Fact]
    public void NextStep_DoesNotProceed_IfNoPackSelected()
    {
        // Arrange
        _packLoader.GetAllPacks().Returns(new[] 
        {
            new DomainPack("generic", new Dictionary<string,string>{{"en", "Generic"}}, new Dictionary<string,string>{{"en", "Generic description"}}, false, Array.Empty<string>(), new Dictionary<string, PackTerminology>(), Array.Empty<PackRule>())
        });

        var cut = RenderComponent<CreateGame>();

        // Act - clicking continue without selecting
        cut.Find("button.cg-btn-primary").Click();

        // Assert - still on step 1
        cut.Markup.ShouldContain("CreateGameStep1Title");
        cut.Markup.ShouldNotContain("CreateGameStep2Title");
    }
}
