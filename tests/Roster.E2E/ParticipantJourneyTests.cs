using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Roster.E2E;

/// <summary>
/// Join → roster → live leaderboard → points → leave, with two domain packs: people (a conference)
/// and teams (a tournament), to prove the participant experience has no context-specific assumptions.
/// </summary>
[Collection(E2EGroup.Name)]
public sealed class ParticipantJourneyTests(RosterAppFixture app)
{
    public static TheoryData<string> Scenarios() => new() { "conference", "tournament" };

    [Theory]
    [MemberData(nameof(Scenarios))]
    public async Task Participant_joins_builds_a_roster_and_follows_the_game(string scenarioName)
    {
        var scenario = scenarioName == "conference" ? TestScenario.Conference : TestScenario.Tournament;
        var game = await TestData.SeedGameAsync(app.ConnectionString, scenario, TestGameState.Open);
        var page = await app.NewPageAsync();

        // Join with the code, then a nickname.
        await page.GotoAsync("/j");
        await page.GetByLabel("Codice partita").FillAsync(game.JoinCode);
        await page.GetByRole(AriaRole.Button, new() { Name = "Vai al gioco" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = scenario.GameName })).ToBeVisibleAsync();
        await app.ScreenshotAsync(page, $"{scenarioName}-01-join");
        await page.GetByLabel("Nickname").FillAsync("FilaTre");
        await page.GetByRole(AriaRole.Button, new() { Name = "Entra nel gioco" }).ClickAsync();

        // Before the start only Roster and Rules are available.
        await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex($"/games/{game.Id}/lineup$"));
        var tabs = page.GetByRole(AriaRole.Navigation, new() { Name = "Sezioni della partita" }).GetByRole(AriaRole.Link);
        await Expect(tabs).ToHaveTextAsync(["Rosa", "Regole"]);

        // Pick three: the first one becomes captain, the save button unlocks.
        var save = page.GetByRole(AriaRole.Button, new() { Name = "Salva la rosa" });
        await Expect(save).ToBeDisabledAsync();
        foreach (var element in scenario.Elements.Take(3))
        {
            await page.GetByRole(AriaRole.Button, new() { Name = element.Name }).ClickAsync();
        }
        await Expect(page.GetByText($"Capitano: {scenario.Elements[0].Name}")).ToBeVisibleAsync();
        await app.ScreenshotAsync(page, $"{scenarioName}-02-roster", fullPage: true);
        await save.ClickAsync();
        await Expect(page.GetByText("Rosa salvata")).ToBeVisibleAsync();
        await app.ScreenshotAsync(page, $"{scenarioName}-03-saved", fullPage: true);

        // The organizer starts the game and a referee scores.
        await TestData.SetStateAsync(app.ConnectionString, game.Id, TestGameState.Live);
        await TestData.AddScoresAsync(app.BaseUrl, game);
        await page.GotoAsync($"/games/{game.Id}");

        await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex($"/games/{game.Id}/leaderboard$"));
        await Expect(tabs).ToHaveTextAsync(["Classifica", "Rosa", "Punti", "Regole"]);
        await Expect(page.GetByRole(AriaRole.Region, new() { Name = "La mia posizione" })).ToContainTextAsync("FilaTre");
        await Expect(page.Locator("li", new() { HasText = "FilaTre" }).GetByText("Tu")).ToBeVisibleAsync();
        await app.ScreenshotAsync(page, $"{scenarioName}-04-leaderboard", fullPage: true);

        await tabs.GetByText("Punti").ClickAsync();
        await Expect(page.GetByText(scenario.Rules[0].Label)).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "I miei pick" }).ClickAsync();
        await Expect(page.GetByText(scenario.Elements[0].Name)).ToBeVisibleAsync();
        await app.ScreenshotAsync(page, $"{scenarioName}-05-feed", fullPage: true);

        await tabs.GetByText("Regole").ClickAsync();
        await Expect(page.GetByText(scenario.Rules[2].Label)).ToBeVisibleAsync();

        // Leave from the avatar menu, with confirmation.
        await page.GetByRole(AriaRole.Button, new() { Name = "Menu partita" }).ClickAsync();
        await app.ScreenshotAsync(page, $"{scenarioName}-06-menu");
        await page.GetByRole(AriaRole.Button, new() { Name = "Esci dalla partita" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Sì, esci" }).ClickAsync();
        await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/j$"));
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public async Task Final_results_show_the_podium(string scenarioName)
    {
        var scenario = scenarioName == "conference" ? TestScenario.Conference : TestScenario.Tournament;
        var game = await TestData.SeedGameAsync(app.ConnectionString, scenario, TestGameState.Open);
        var page = await app.NewPageAsync(colorScheme: ColorScheme.Dark);
        await page.GotoAsync($"/games/{game.Id}/join");
        await page.GetByLabel("Nickname").FillAsync("FilaTre");
        await page.GetByRole(AriaRole.Button, new() { Name = "Entra nel gioco" }).ClickAsync();
        await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/lineup$"));

        await TestData.SetStateAsync(app.ConnectionString, game.Id, TestGameState.Live);
        await TestData.AddScoresAsync(app.BaseUrl, game);
        await TestData.SetStateAsync(app.ConnectionString, game.Id, TestGameState.Ended);
        await page.GotoAsync($"/games/{game.Id}/leaderboard");

        await Expect(page.GetByText("Partita finita")).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.List, new() { Name = "Podio" }).GetByRole(AriaRole.Listitem)).ToHaveCountAsync(3);
        await app.ScreenshotAsync(page, $"{scenarioName}-07-final-dark", fullPage: true);
    }
}
