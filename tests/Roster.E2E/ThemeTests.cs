using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Roster.E2E;

[Collection(E2EGroup.Name)]
public sealed class ThemeTests(RosterAppFixture app)
{
    [Theory]
    [InlineData(ColorScheme.Light, "light")]
    [InlineData(ColorScheme.Dark, "dark")]
    public async Task System_preference_follows_the_device_color_scheme(ColorScheme scheme, string expected)
    {
        var page = await app.NewPageAsync(colorScheme: scheme);

        await page.GotoAsync("/");

        var html = page.Locator("html");
        await Expect(html).ToHaveAttributeAsync("data-theme-preference", "system");
        await Expect(html).ToHaveAttributeAsync("data-theme", expected);
        await Expect(html).ToHaveAttributeAsync("data-bs-theme", expected);
    }

    [Fact]
    public async Task Theme_toggle_cycles_system_light_dark_and_survives_a_reload()
    {
        var page = await app.NewPageAsync(colorScheme: ColorScheme.Dark);
        await page.GotoAsync("/");
        var html = page.Locator("html");

        await page.Locator("summary[aria-label=Menu]").ClickAsync();
        var toggle = page.Locator(".pub-menu__settings [data-theme-cycle]");
        await Expect(toggle).ToHaveTextAsync("Sistema");

        await toggle.ClickAsync();
        await Expect(html).ToHaveAttributeAsync("data-theme", "light");
        await Expect(toggle).ToHaveTextAsync("Chiaro");
        await Expect(toggle).ToHaveAttributeAsync("aria-label", "Tema: Chiaro. Cambia tema");

        await toggle.ClickAsync();
        await Expect(html).ToHaveAttributeAsync("data-theme", "dark");
        await Expect(toggle).ToHaveTextAsync("Scuro");

        await toggle.ClickAsync();
        await Expect(html).ToHaveAttributeAsync("data-theme-preference", "system");
        await Expect(html).ToHaveAttributeAsync("data-theme", "dark");

        await toggle.ClickAsync();
        await page.ReloadAsync();
        await Expect(html).ToHaveAttributeAsync("data-theme-preference", "light");
        await Expect(html).ToHaveAttributeAsync("data-theme", "light");
    }

    [Fact]
    public async Task Footer_toggle_shows_the_current_preference()
    {
        var page = await app.NewPageAsync(width: 1280, height: 800);
        await page.GotoAsync("/");

        var toggle = page.Locator(".pub-footer__bottom [data-theme-cycle]");
        await Expect(toggle).ToHaveTextAsync("Sistema");
        await toggle.ClickAsync();
        await Expect(toggle).ToHaveTextAsync("Chiaro");
    }
}
