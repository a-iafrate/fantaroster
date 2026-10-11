using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Roster.E2E;

/// <summary>Each client surface has its own layout, theme and interactivity.</summary>
[Collection(E2EGroup.Name)]
public sealed class SurfaceTests(RosterAppFixture app)
{
    private static readonly string UnknownGame = Guid.Empty.ToString();

    [Fact]
    public async Task Participant_pages_have_no_public_site_chrome_and_follow_the_device_theme()
    {
        var page = await app.NewPageAsync(colorScheme: ColorScheme.Dark);

        await page.GotoAsync("/j");

        await Expect(page.Locator("[data-theme-surface=participant]")).ToBeVisibleAsync();
        await Expect(page.Locator(".pub-header")).ToHaveCountAsync(0);
        await Expect(page.Locator("html")).ToHaveAttributeAsync("data-theme", "dark");
    }

    [Fact]
    public async Task Referee_console_is_interactive_and_dark_by_default()
    {
        var page = await app.NewPageAsync(colorScheme: ColorScheme.Light);

        await page.GotoAsync($"/games/{UnknownGame}/referee");

        // Prerendering is off: the console markup only exists once WebAssembly is running.
        await Expect(page.Locator("[data-theme-surface=referee]")).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await Expect(page.Locator(".pub-header")).ToHaveCountAsync(0);
        await Expect(page.Locator("html")).ToHaveAttributeAsync("data-theme", "dark");
    }

    [Fact]
    public async Task Big_screen_is_interactive_and_always_uses_the_stage_theme()
    {
        var page = await app.NewPageAsync(width: 1920, height: 1080, colorScheme: ColorScheme.Dark);

        await page.GotoAsync($"/games/{UnknownGame}/big-screen");

        await Expect(page.Locator(".stage")).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await Expect(page.Locator(".pub-header")).ToHaveCountAsync(0);
        await Expect(page.Locator("html")).ToHaveAttributeAsync("data-theme", "stage");
    }
}
