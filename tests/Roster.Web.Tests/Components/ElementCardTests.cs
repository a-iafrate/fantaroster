using Bunit;
using Microsoft.AspNetCore.Components;
using Roster.Ui.Components;
using Shouldly;
using Xunit;

namespace Roster.Web.Tests.Components;

public sealed class ElementCardTests : TestContext
{
    private IRenderedComponent<ElementCard> Render(ElementPickState state, bool disabled = false, Action? onToggle = null, Action? onCaptain = null) =>
        RenderComponent<ElementCard>(p => p
            .Add(c => c.Name, "Red Lions")
            .Add(c => c.Subtitle, "Group A, Bologna")
            .Add(c => c.State, state)
            .Add(c => c.CaptainEnabled, true)
            .Add(c => c.CaptainLabel, "×2")
            .Add(c => c.MakeCaptainLabel, "Make Red Lions captain")
            .Add(c => c.IsDisabled, disabled)
            .Add(c => c.OnToggle, EventCallback.Factory.Create(this, () => onToggle?.Invoke()))
            .Add(c => c.OnMakeCaptain, EventCallback.Factory.Create(this, () => onCaptain?.Invoke())));

    [Fact]
    public void Free_element_shows_the_add_icon_and_is_not_pressed()
    {
        var cut = Render(ElementPickState.Free);

        cut.Find(".pick__main").GetAttribute("aria-pressed").ShouldBe("false");
        cut.Find(".pick__state i").ClassList.ShouldContain("icon-plus");
        cut.FindAll(".pick__star").Count.ShouldBe(0);
    }

    [Fact]
    public void Picked_element_can_become_captain()
    {
        var captain = 0;
        var cut = Render(ElementPickState.Picked, onCaptain: () => captain++);

        cut.Find(".pick__main").GetAttribute("aria-pressed").ShouldBe("true");
        cut.Find(".pick__star").GetAttribute("aria-label").ShouldBe("Make Red Lions captain");
        cut.Find(".pick__star").Click();

        captain.ShouldBe(1);
    }

    [Fact]
    public void Captain_shows_the_multiplier_and_no_star_button()
    {
        var cut = Render(ElementPickState.Captain);

        cut.Find(".pick__captain").TextContent.ShouldContain("×2");
        cut.FindAll(".pick__star").Count.ShouldBe(0);
        cut.FindAll(".pick__backdrop").Count.ShouldBe(1);
    }

    [Fact]
    public void Clicking_the_card_toggles_the_pick()
    {
        var toggles = 0;
        var cut = Render(ElementPickState.Free, onToggle: () => toggles++);

        cut.Find(".pick__main").Click();

        toggles.ShouldBe(1);
    }

    [Fact]
    public void Disabled_card_ignores_clicks_and_says_so_to_assistive_technology()
    {
        var toggles = 0;
        var cut = Render(ElementPickState.Free, disabled: true, onToggle: () => toggles++);

        cut.Find(".pick__main").GetAttribute("aria-disabled").ShouldBe("true");
        cut.Find(".pick__main").Click();

        toggles.ShouldBe(0);
    }
}
