using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Shouldly;
using static Microsoft.Playwright.Assertions;

namespace Roster.E2E;

/// <summary>
/// The referee console: invite link, three taps to assign a point, undo, offline queue.
/// Run with two domain packs (people and teams).
/// </summary>
[Collection(E2EGroup.Name)]
public sealed class RefereeJourneyTests(RosterAppFixture app)
{
    public static TheoryData<string> Scenarios() => new() { "conference", "tournament" };

    private static TestScenario Scenario(string name) => name == "conference" ? TestScenario.Conference : TestScenario.Tournament;

    private static string InviteUrl(SeededGame game) => $"/games/{game.Id}/referee#t={game.RefereeToken}";

    [Theory]
    [MemberData(nameof(Scenarios))]
    public async Task Referee_assigns_a_point_in_three_taps_and_can_undo_it(string scenarioName)
    {
        var scenario = Scenario(scenarioName);
        var game = await TestData.SeedGameAsync(app.ConnectionString, scenario, TestGameState.Live);
        var page = await app.NewPageAsync();

        await page.GotoAsync(InviteUrl(game));

        // The token is stored on the device and removed from the address bar.
        var element = scenario.Elements[0];
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = element.Name })).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await Expect(page).Not.ToHaveURLAsync(new Regex("#t="));
        (await page.EvaluateAsync<string?>($"() => localStorage.getItem('refereeToken.{game.Id}')")).ShouldBe(game.RefereeToken);
        await Expect(page.Locator("html")).ToHaveAttributeAsync("data-theme", "dark");
        await app.ScreenshotAsync(page, $"referee-{scenarioName}-1-element");

        // Tap 1: who. Tap 2: what. Tap 3: confirm.
        await page.GetByRole(AriaRole.Button, new() { Name = element.Name }).ClickAsync();
        var rule = scenario.Rules[0];
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Cosa è successo?" })).ToBeVisibleAsync();
        await app.ScreenshotAsync(page, $"referee-{scenarioName}-2-rule");
        await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex(Regex.Escape(rule.Label)) }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Conferma" })).ToBeVisibleAsync();
        await app.ScreenshotAsync(page, $"referee-{scenarioName}-3-confirm");
        await page.GetByRole(AriaRole.Button, new() { Name = $"Assegna +{rule.Points}" }).ClickAsync();

        // Back to the start, with a ten second undo.
        var undo = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex(@"^Annulla \d+ s$") });
        await Expect(undo).ToBeVisibleAsync();
        await app.ScreenshotAsync(page, $"referee-{scenarioName}-4-undo");
        (await ValidEntriesAsync(game)).ShouldBe(1);

        await undo.ClickAsync();
        await Expect(undo).ToHaveCountAsync(0);
        await WaitForValidEntriesAsync(game, 0);

        // The voided point stays in the history.
        await page.GetByRole(AriaRole.Button, new() { Name = "Menu" }).ClickAsync();
        await app.ScreenshotAsync(page, $"referee-{scenarioName}-5-menu");
        await page.GetByRole(AriaRole.Button, new() { Name = "Punti recenti" }).ClickAsync();
        await Expect(page.GetByText("annullato")).ToBeVisibleAsync();
        await app.ScreenshotAsync(page, $"referee-{scenarioName}-6-recent");
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public async Task Points_assigned_offline_are_queued_and_sent_once_when_the_connection_is_back(string scenarioName)
    {
        var scenario = Scenario(scenarioName);
        var game = await TestData.SeedGameAsync(app.ConnectionString, scenario, TestGameState.Live);
        var page = await app.NewPageAsync();
        await page.GotoAsync(InviteUrl(game));
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = scenario.Elements[0].Name })).ToBeVisibleAsync(new() { Timeout = 30_000 });

        await page.Context.SetOfflineAsync(true);
        await page.GetByRole(AriaRole.Button, new() { Name = scenario.Elements[0].Name }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex(Regex.Escape(scenario.Rules[1].Label)) }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = $"Assegna +{scenario.Rules[1].Points}" }).ClickAsync();

        await Expect(page.GetByText("Offline, punti in attesa: 1").First).ToBeVisibleAsync();
        await app.ScreenshotAsync(page, $"referee-{scenarioName}-7-offline");
        (await ValidEntriesAsync(game)).ShouldBe(0);

        // Back online: the console retries by itself, and the server keeps exactly one point.
        await page.Context.SetOfflineAsync(false);
        await Expect(page.GetByText("Offline, punti in attesa: 1")).ToHaveCountAsync(0, new() { Timeout = 40_000 });
        await WaitForValidEntriesAsync(game, 1);
    }

    [Fact]
    public async Task Without_a_valid_invite_the_console_asks_for_the_link()
    {
        var game = await TestData.SeedGameAsync(app.ConnectionString, TestScenario.Conference, TestGameState.Live);
        var page = await app.NewPageAsync();

        await page.GotoAsync($"/games/{game.Id}/referee#t=not-a-real-token");

        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Serve un link di invito" })).ToBeVisibleAsync(new() { Timeout = 30_000 });
        (await page.EvaluateAsync<string?>($"() => localStorage.getItem('refereeToken.{game.Id}')")).ShouldBeNull();
        await app.ScreenshotAsync(page, "referee-invalid-invite");
    }

    [Fact]
    public async Task Before_the_start_the_console_explains_that_points_cannot_be_assigned_yet()
    {
        var game = await TestData.SeedGameAsync(app.ConnectionString, TestScenario.Tournament, TestGameState.Open);
        var page = await app.NewPageAsync();

        await page.GotoAsync(InviteUrl(game));

        await Expect(page.GetByText("La partita non è ancora iniziata")).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = TestScenario.Tournament.Elements[0].Name })).ToBeDisabledAsync();
    }

    /// <summary>The server finishes requests after the UI has moved on: wait for the expected count.</summary>
    private async Task WaitForValidEntriesAsync(SeededGame game, int expected)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        var count = await ValidEntriesAsync(game);
        while (count != expected && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(200);
            count = await ValidEntriesAsync(game);
        }
        count.ShouldBe(expected);
    }

    private async Task<int> ValidEntriesAsync(SeededGame game)
    {
        using var http = new HttpClient { BaseAddress = app.BaseUrl };
        var entries = await http.GetFromJsonAsync<List<EntryStatus>>($"/api/games/{game.Id}/score-entries?count=50") ?? [];
        return entries.Count(e => e.Status == "Valid");
    }

    private sealed record EntryStatus(string Status);
}
