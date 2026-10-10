using System.Globalization;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Roster.Web.Client.Shared;
using Shouldly;
using Xunit;

namespace Roster.Web.Tests.Components;

public sealed class ThemeToggleTests : TestContext
{
    public ThemeToggleTests()
    {
        Services.AddLocalization(options => options.ResourcesPath = "Resources");
    }

    [Theory]
    [InlineData("en", "System", "Light", "Dark", "Theme: System. Change theme")]
    [InlineData("it", "Sistema", "Chiaro", "Scuro", "Tema: Sistema. Cambia tema")]
    public void ThemeToggle_RendersLocalizedLabelsForTheThemeScript(
        string culture, string system, string light, string dark, string ariaLabel)
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
        try
        {
            var cut = RenderComponent<ThemeToggle>(parameters => parameters.Add(p => p.CssClass, "pub-chip"));

            var button = cut.Find("button[data-theme-cycle]");
            button.GetAttribute("type").ShouldBe("button");
            button.ClassName.ShouldBe("pub-chip");
            button.GetAttribute("data-label-system").ShouldBe(system);
            button.GetAttribute("data-label-light").ShouldBe(light);
            button.GetAttribute("data-label-dark").ShouldBe(dark);
            button.GetAttribute("aria-label").ShouldBe(ariaLabel);
            button.GetAttribute("data-aria-template").ShouldBe(ariaLabel.Replace(system, "{0}", StringComparison.Ordinal));
            cut.Find("[data-theme-label]").TextContent.ShouldBe(system);
            cut.Find("[data-theme-icon]").GetAttribute("aria-hidden").ShouldBe("true");
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
