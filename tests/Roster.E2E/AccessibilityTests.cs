using System.Text.RegularExpressions;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;
using Shouldly;
using static Microsoft.Playwright.Assertions;

namespace Roster.E2E;

/// <summary>
/// WCAG 2.2 AA audit with axe-core on every surface, light and dark, on a phone and on a laptop. Each test walks the
/// states of one surface and fails with the list of rules and elements that break.
/// </summary>
[Collection(E2EGroup.Name)]
public sealed partial class AccessibilityTests(RosterAppFixture app)
{
    private static readonly AxeRunOptions RunOptions = new()
    {
        RunOnly = new RunOnlyOptions { Type = "tag", Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "wcag22aa"] },
    };

    private static readonly string[] OrganizerSections = ["", "/elements", "/rules", "/referees", "/control", "/materials", "/settings"];

    public static TheoryData<int, ColorScheme> Setups() => new()
    {
        { 390, ColorScheme.Light },
        { 390, ColorScheme.Dark },
        { 1280, ColorScheme.Light },
        { 1280, ColorScheme.Dark },
    };

    private static int HeightFor(int width) => width < 1000 ? 844 : 800;

    private static async Task AuditAsync(IPage page, string where, List<string> problems)
    {
        // Colors are measured at rest: with reduced motion (which the app honors) nothing is mid-transition.
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.Reduce });
        var results = await page.RunAxe(RunOptions);
        foreach (var violation in results.Violations)
        {
            var nodes = string.Join("; ", violation.Nodes.Take(4).Select(n => n.Target));
            problems.Add($"[{where}] {violation.Id} ({violation.Impact}): {violation.Help} -> {nodes} ({violation.Nodes.Length} element(s))");
        }
    }

    private static void ShouldBeClean(List<string> problems) =>
        problems.ShouldBeEmpty(Environment.NewLine + string.Join(Environment.NewLine, problems));

    [Theory]
    [MemberData(nameof(Setups))]
    public async Task Public_pages_have_no_violations(int width, ColorScheme scheme)
    {
        var problems = new List<string>();
        var page = await app.NewPageAsync(width, HeightFor(width), scheme);

        foreach (var path in new[] { "/", "/login", "/login?error=invalid_link", "/j", "/terms", "/privacy", "/not-found" })
        {
            await page.GotoAsync(path);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await AuditAsync(page, $"{path} {width} {scheme}", problems);
        }

        ShouldBeClean(problems);
    }

    [Theory]
    [MemberData(nameof(Setups))]
    public async Task Participant_screens_have_no_violations(int width, ColorScheme scheme)
    {
        var problems = new List<string>();
        var scenario = TestScenario.Conference;
        var game = await TestData.SeedGameAsync(app.ConnectionString, scenario, TestGameState.Open);
        var page = await app.NewPageAsync(width, HeightFor(width), scheme);

        await page.GotoAsync("/j");
        await page.GetByLabel("Codice partita").FillAsync(game.JoinCode);
        await page.GetByRole(AriaRole.Button, new() { Name = "Vai al gioco" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = scenario.GameName })).ToBeVisibleAsync();
        await AuditAsync(page, $"join {width} {scheme}", problems);

        await page.GetByLabel("Nickname").FillAsync("FilaTre");
        await page.GetByRole(AriaRole.Button, new() { Name = "Entra nel gioco" }).ClickAsync();
        await Expect(page).ToHaveURLAsync(new Regex($"/games/{game.Id}/lineup$"));
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = scenario.Elements[0].Name })).ToBeVisibleAsync();
        await AuditAsync(page, $"lineup {width} {scheme}", problems);

        foreach (var element in scenario.Elements.Take(3))
        {
            await page.GetByRole(AriaRole.Button, new() { Name = element.Name }).ClickAsync();
        }
        await AuditAsync(page, $"lineup picked {width} {scheme}", problems);
        await page.GetByRole(AriaRole.Button, new() { Name = "Salva la rosa" }).ClickAsync();
        await Expect(page.GetByText("Rosa salvata")).ToBeVisibleAsync();

        await TestData.SetStateAsync(app.ConnectionString, game.Id, TestGameState.Live);
        await TestData.AddScoresAsync(app.BaseUrl, game);
        foreach (var section in new[] { "leaderboard", "lineup", "activity", "rules" })
        {
            await page.GotoAsync($"/games/{game.Id}/{section}");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Expect(page.Locator("[data-theme-surface=participant]")).ToBeVisibleAsync();
            await AuditAsync(page, $"{section} {width} {scheme}", problems);
        }

        ShouldBeClean(problems);
    }

    [Theory]
    [MemberData(nameof(Setups))]
    public async Task Referee_console_has_no_violations(int width, ColorScheme scheme)
    {
        var problems = new List<string>();
        var scenario = TestScenario.Tournament;
        var game = await TestData.SeedGameAsync(app.ConnectionString, scenario, TestGameState.Live);
        var page = await app.NewPageAsync(width, HeightFor(width), scheme);

        await page.GotoAsync($"/games/{game.Id}/referee#t={game.RefereeToken}");
        var element = scenario.Elements[0];
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = element.Name })).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await AuditAsync(page, $"referee element {width} {scheme}", problems);

        await page.GetByRole(AriaRole.Button, new() { Name = element.Name }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Cosa è successo?" })).ToBeVisibleAsync();
        await AuditAsync(page, $"referee rule {width} {scheme}", problems);

        await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex(Regex.Escape(scenario.Rules[0].Label)) }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Conferma" })).ToBeVisibleAsync();
        await AuditAsync(page, $"referee confirm {width} {scheme}", problems);

        ShouldBeClean(problems);
    }

    [Theory]
    [MemberData(nameof(Setups))]
    public async Task Organizer_console_has_no_violations(int width, ColorScheme scheme)
    {
        var problems = new List<string>();
        var game = await TestData.SeedGameAsync(app.ConnectionString, TestScenario.Conference, TestGameState.Open);
        var page = await app.NewOrganizerPageAsync(width, HeightFor(width), scheme, game.Id);

        var paths = new List<string> { "/organizer", "/organizer/account", "/organizer/create-game" };
        paths.AddRange(OrganizerSections.Select(s => $"/organizer/games/{game.Id}{s}"));

        foreach (var path in paths)
        {
            await page.GotoAsync(path);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForFunctionAsync("() => !!document.querySelector('[data-theme-surface], main, .ds-page')");
            await AuditAsync(page, $"{path} {width} {scheme}", problems);
        }

        // The account menu, open.
        await page.GotoAsync("/organizer");
        if (width < 1000)
        {
            // On a phone the navigation, with the account button, lives in a drawer.
            var drawer = page.Locator("#org-sidebar");
            for (var attempt = 0; attempt < 10 && !await drawer.IsVisibleAsync(); attempt++)
            {
                await page.GetByRole(AriaRole.Button, new() { Name = "Apri il menu" }).ClickAsync();
                await page.WaitForTimeoutAsync(500);
            }
            await Expect(drawer).ToBeVisibleAsync();
            await AuditAsync(page, $"drawer {width} {scheme}", problems);
        }

        await page.Locator("summary.org-account-btn").ClickAsync();
        await Expect(page.GetByRole(AriaRole.Group, new() { Name = "Menu account" })).ToBeVisibleAsync();
        await AuditAsync(page, $"account menu {width} {scheme}", problems);

        ShouldBeClean(problems);
    }

    [Theory]
    [InlineData(ColorScheme.Light)]
    [InlineData(ColorScheme.Dark)]
    public async Task Big_screen_has_no_violations(ColorScheme scheme)
    {
        var problems = new List<string>();
        var game = await TestData.SeedGameAsync(app.ConnectionString, TestScenario.Conference, TestGameState.Live);
        await TestData.AddScoresAsync(app.BaseUrl, game);
        var page = await app.NewPageAsync(1920, 1080, scheme);

        await page.GotoAsync($"/games/{game.Id}/big-screen");
        await Expect(page.Locator(".stage")).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await AuditAsync(page, $"big screen {scheme}", problems);

        ShouldBeClean(problems);
    }
}
