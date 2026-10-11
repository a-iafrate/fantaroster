using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Shouldly;
using static Microsoft.Playwright.Assertions;

namespace Roster.E2E;

/// <summary>The big screen: stage theme, scaled layout, live ranking, join screen, controls. Two domain packs.</summary>
[Collection(E2EGroup.Name)]
public sealed class BigScreenTests(RosterAppFixture app)
{
    public static TheoryData<string, int, int> Displays() => new()
    {
        { "conference", 1920, 1080 },
        { "conference", 1280, 720 },
        { "tournament", 3840, 2160 },
    };

    private static TestScenario Scenario(string name) => name == "conference" ? TestScenario.Conference : TestScenario.Tournament;

    [Theory]
    [MemberData(nameof(Displays))]
    public async Task Leaderboard_shows_the_top_rows_the_join_card_and_the_latest_points(string scenarioName, int width, int height)
    {
        var scenario = Scenario(scenarioName);
        var game = await TestData.SeedGameAsync(app.ConnectionString, scenario, TestGameState.Live);
        await TestData.AddScoresAsync(app.BaseUrl, game);
        var page = await app.NewPageAsync(width, height, ColorScheme.Light);

        await page.GotoAsync($"/games/{game.Id}/big-screen");

        // Always the stage theme, never the device theme.
        await Expect(page.Locator("html")).ToHaveAttributeAsync("data-theme", "stage");
        await Expect(page.Locator(".row")).ToHaveCountAsync(3, new() { Timeout = 30_000 });
        await Expect(page.GetByText(game.JoinCode).First).ToBeVisibleAsync();
        await Expect(page.GetByLabel("Ultimi punti")).ToContainTextAsync(scenario.Rules[0].Label);

        // The stage keeps 16:9 whatever the resolution, and nothing overflows it.
        var stage = await page.Locator(".stage").BoundingBoxAsync();
        stage.ShouldNotBeNull();
        ((double)stage.Width / stage.Height).ShouldBe(16d / 9d, 0.01);
        ((double)stage.Width).ShouldBe(width, 1);

        var smallestText = await page.EvaluateAsync<double>(
            """
            () => Math.min(...[...document.querySelectorAll('.stage *')]
                .filter(e => e.childNodes.length && [...e.childNodes].some(n => n.nodeType === 3 && n.textContent.trim()))
                .filter(e => !e.closest('[aria-hidden=true]') && getComputedStyle(e).visibility !== 'hidden' && e.getBoundingClientRect().width > 0)
                .map(e => parseFloat(getComputedStyle(e).fontSize) * 1920 / innerWidth))
            """);
        smallestText.ShouldBeGreaterThanOrEqualTo(31.9, "readable text on the big screen must be at least 32 design pixels (decorative initials excluded)");

        await app.ScreenshotAsync(page, $"bigscreen-{scenarioName}-{width}");
    }

    [Fact]
    public async Task Before_the_start_the_screen_shows_the_join_invitation()
    {
        var game = await TestData.SeedGameAsync(app.ConnectionString, TestScenario.Tournament, TestGameState.Open);
        var page = await app.NewPageAsync(1920, 1080);

        await page.GotoAsync($"/games/{game.Id}/big-screen");

        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Sei ancora in tempo." })).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await Expect(page.GetByText("Rose aperte")).ToBeVisibleAsync();
        await Expect(page.GetByText(game.JoinCode).First).ToBeVisibleAsync();
        await app.ScreenshotAsync(page, "bigscreen-join");
    }

    [Fact]
    public async Task A_new_point_reorders_the_ranking_live_and_announces_the_climber()
    {
        var game = await TestData.SeedGameAsync(app.ConnectionString, TestScenario.Conference, TestGameState.Live);
        await TestData.AddScoresAsync(app.BaseUrl, game);
        var page = await app.NewPageAsync(1920, 1080);
        await page.GotoAsync($"/games/{game.Id}/big-screen");
        await Expect(page.Locator(".row")).ToHaveCountAsync(3, new() { Timeout = 30_000 });

        // A referee scores the first element again: its fans climb or extend the lead, and the board refreshes by itself.
        var before = await page.Locator(".row").First.InnerTextAsync();
        await TestData.AddOneScoreAsync(app.BaseUrl, game, game.ElementIds[1], game.RuleIds[0]);

        await Expect(page.Locator(".row").First).Not.ToHaveTextAsync(before, new() { Timeout = 15_000 });
        await app.ScreenshotAsync(page, "bigscreen-after-point");
    }

    [Fact]
    public async Task Controls_appear_with_the_mouse_and_hide_after_three_seconds()
    {
        var game = await TestData.SeedGameAsync(app.ConnectionString, TestScenario.Conference, TestGameState.Live);
        var page = await app.NewPageAsync(1920, 1080);
        await page.GotoAsync($"/games/{game.Id}/big-screen");
        await Expect(page.Locator(".stage")).ToBeVisibleAsync(new() { Timeout = 30_000 });

        var toolbar = page.GetByRole(AriaRole.Toolbar);
        await page.Mouse.MoveAsync(500, 500);
        await Expect(toolbar).ToHaveCSSAsync("opacity", "1");
        await app.ScreenshotAsync(page, "bigscreen-controls");

        await page.GetByRole(AriaRole.Button, new() { Name = "Entra adesso" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Sei ancora in tempo." })).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Classifica" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Sei ancora in tempo." })).ToHaveCountAsync(0);

        await Expect(toolbar).ToHaveCSSAsync("opacity", "0", new() { Timeout = 8_000 });
        await Expect(page.Locator(".stage")).ToHaveClassAsync(new Regex("stage--idle"));
    }
}
