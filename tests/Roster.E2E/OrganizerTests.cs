using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Shouldly;
using static Microsoft.Playwright.Assertions;

namespace Roster.E2E;

/// <summary>
/// The organizer console on a phone and on a laptop: every section loads, nothing overflows horizontally,
/// the menu works. Run with two domain packs (people and teams).
/// </summary>
[Collection(E2EGroup.Name)]
public sealed class OrganizerTests(RosterAppFixture app)
{
    public static TheoryData<string, int, int> Devices() => new()
    {
        { "conference", 390, 844 },
        { "conference", 1280, 800 },
        { "tournament", 390, 844 },
    };

    [Theory]
    [MemberData(nameof(Devices))]
    public async Task Every_section_fits_the_screen(string scenarioName, int width, int height)
    {
        var scenario = scenarioName == "conference" ? TestScenario.Conference : TestScenario.Tournament;
        var game = await TestData.SeedGameAsync(app.ConnectionString, scenario, TestGameState.Open);
        var page = await app.NewOrganizerPageAsync(width, height, ownedGames: game.Id);
        var tag = $"organizer-{scenarioName}-{width}";

        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Le mie partite" })).ToBeVisibleAsync();
        await WaitForInteractiveAsync(page);
        await AssertFitsAsync(page, "/organizer");
        await app.ScreenshotAsync(page, $"{tag}-dashboard", fullPage: true);

        foreach (var (section, heading) in new[]
        {
            ("", "Panoramica"), ("elements", "Elementi"), ("rules", "Regole"), ("referees", "Arbitri"),
            ("control", "Controllo"), ("materials", "Materiali"), ("settings", "Impostazioni"),
        })
        {
            await page.GotoAsync($"/organizer/games/{game.Id}/{section}");
            await Expect(page.GetByRole(AriaRole.Heading, new() { Name = heading, Level = 1 })).ToBeVisibleAsync();
            await WaitForInteractiveAsync(page);
            await AssertFitsAsync(page, $"/{section}");
            await app.ScreenshotAsync(page, $"{tag}-{(section.Length == 0 ? "overview" : section)}", fullPage: true);
        }
    }

    [Fact]
    public async Task Drawer_opens_navigates_and_closes_on_a_phone()
    {
        var game = await TestData.SeedGameAsync(app.ConnectionString, TestScenario.Conference, TestGameState.Open);
        var page = await app.NewOrganizerPageAsync(390, 844, ColorScheme.Dark, game.Id);
        await page.GotoAsync($"/organizer/games/{game.Id}");
        await WaitForInteractiveAsync(page);

        var drawer = page.Locator("#org-sidebar");
        await Expect(drawer).ToBeHiddenAsync();
        await ClickUntilVisibleAsync(page.GetByRole(AriaRole.Button, new() { Name = "Apri il menu" }), drawer);
        await page.WaitForTimeoutAsync(500); // slide-in animation
        await app.ScreenshotAsync(page, "organizer-drawer-dark");

        await drawer.GetByRole(AriaRole.Link, new() { Name = "Regole" }).ClickAsync();
        await Expect(page).ToHaveURLAsync(new Regex("/rules$"));
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Regole", Level = 1 })).ToBeVisibleAsync();
        await Expect(drawer).ToBeHiddenAsync();

        await ClickUntilVisibleAsync(page.GetByRole(AriaRole.Button, new() { Name = "Apri il menu" }), drawer);
        await page.Keyboard.PressAsync("Escape");
        await Expect(drawer).ToBeHiddenAsync();
    }

    [Fact]
    public async Task Account_menu_changes_theme_and_signs_out()
    {
        var page = await app.NewOrganizerPageAsync(1280, 800);
        await WaitForInteractiveAsync(page);

        await page.Locator("summary.org-account-btn").ClickAsync();
        await Expect(page.GetByRole(AriaRole.Group, new() { Name = "Menu account" })).ToBeVisibleAsync();
        var theme = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Tema") });
        await theme.ClickAsync();
        await Expect(page.Locator("html")).ToHaveAttributeAsync("data-theme-preference", "light");
        await app.ScreenshotAsync(page, "organizer-account-menu");

        await page.GetByRole(AriaRole.Button, new() { Name = "Esci" }).ClickAsync();
        await Expect(page).ToHaveURLAsync(new Regex("/$"));
        await page.GotoAsync("/organizer");
        await Expect(page).ToHaveURLAsync(new Regex("/login"));
    }

    [Fact]
    public async Task Rules_are_edited_in_a_dialog_with_bonus_malus_and_a_stepper()
    {
        var game = await TestData.SeedGameAsync(app.ConnectionString, TestScenario.Tournament, TestGameState.Open);
        var page = await app.NewOrganizerPageAsync(390, 844, ownedGames: game.Id);
        await page.GotoAsync($"/organizer/games/{game.Id}/rules");
        await WaitForInteractiveAsync(page);

        var dialog = page.GetByRole(AriaRole.Dialog);
        await ClickUntilVisibleAsync(page.GetByRole(AriaRole.Button, new() { Name = "Aggiungi regola" }).First, dialog);
        await dialog.GetByLabel("Cosa succede").FillAsync("Own goal");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Malus" }).ClickAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Più punti" }).ClickAsync();
        await Expect(dialog.Locator("output")).ToHaveTextAsync("−2");
        await app.ScreenshotAsync(page, "organizer-rule-dialog");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Salva regola" }).ClickAsync();

        var row = page.Locator("li", new() { HasText = "Own goal" });
        await Expect(row).ToBeVisibleAsync();
        await Expect(row.GetByText("−2")).ToBeVisibleAsync();
    }

    [Theory]
    [InlineData(390, 844)]
    [InlineData(1280, 800)]
    public async Task Wizard_creates_a_game_by_hand_in_six_steps(int width, int height)
    {
        var page = await app.NewOrganizerPageAsync(width, height);
        await page.GotoAsync("/organizer/create-game");
        await WaitForInteractiveAsync(page);
        var tag = $"wizard-{width}";

        // 1. What is it about: any pack; the first one is enough for this journey.
        var packs = page.GetByRole(AriaRole.Radiogroup).First.GetByRole(AriaRole.Radio);
        await Expect(packs.First).ToBeVisibleAsync();
        await AssertFitsAsync(page, "wizard step 1");
        await app.ScreenshotAsync(page, $"{tag}-1-pack");
        await packs.Nth(1).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Continua" }).ClickAsync();

        // 2. Settings.
        await page.GetByLabel("Nome della partita").FillAsync("Wizard game");
        await AssertFitsAsync(page, "wizard step 2");
        await app.ScreenshotAsync(page, $"{tag}-2-settings");
        await page.GetByRole(AriaRole.Button, new() { Name = "Continua" }).ClickAsync();

        // 3. Source: by hand (no plugin needed).
        await Expect(page.GetByRole(AriaRole.Radio, new() { NameRegex = new Regex("Nessuna") }).First).ToBeVisibleAsync();
        await AssertFitsAsync(page, "wizard step 3");
        await app.ScreenshotAsync(page, $"{tag}-3-source");
        await page.GetByRole(AriaRole.Radio).Last.ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Importa e continua|Continua") }).ClickAsync();

        // 4 and 5: elements and rules preview.
        await page.GetByRole(AriaRole.Button, new() { Name = "Continua" }).ClickAsync();
        await AssertFitsAsync(page, "wizard step 5");
        await app.ScreenshotAsync(page, $"{tag}-5-rules");
        await page.GetByRole(AriaRole.Button, new() { Name = "Continua" }).ClickAsync();

        // 6. Publish: lands on the new game.
        await app.ScreenshotAsync(page, $"{tag}-6-publish");
        await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Pubblica") }).ClickAsync();
        await Expect(page).ToHaveURLAsync(new Regex(@"/organizer/games/[0-9a-f-]{36}$"));
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Panoramica", Level = 1 })).ToBeVisibleAsync();
    }

    /// <summary>
    /// Pages are prerendered and become interactive when the circuit connects: a click before that does nothing.
    /// Click again until what the click should open is there.
    /// </summary>
    private static async Task ClickUntilVisibleAsync(ILocator trigger, ILocator expected)
    {
        for (var attempt = 0; attempt < 15; attempt++)
        {
            await trigger.ClickAsync();
            if (await expected.First.IsVisibleAsync())
            {
                return;
            }
            await Task.Delay(400);
            if (await expected.First.IsVisibleAsync())
            {
                return;
            }
        }
        await Expect(expected.First).ToBeVisibleAsync();
    }

    /// <summary>The page is interactive once the circuit is up: Blazor marks it by removing the loading state.</summary>
    private static async Task WaitForInteractiveAsync(IPage page) =>
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

    private static async Task AssertFitsAsync(IPage page, string where)
    {
        var overflow = await page.EvaluateAsync<int>("() => document.documentElement.scrollWidth - document.documentElement.clientWidth");
        if (overflow > 0)
        {
            var culprits = await page.EvaluateAsync<string>(
                """
                () => [...document.querySelectorAll('body *')]
                    .filter(e => e.getBoundingClientRect().right > document.documentElement.clientWidth + 1)
                    .slice(0, 5)
                    .map(e => `${e.tagName.toLowerCase()}.${e.className} (${Math.round(e.getBoundingClientRect().width)}px)`)
                    .join(', ')
                """);
            overflow.ShouldBe(0, $"{where} overflows horizontally: {culprits}");
        }
    }

    [Fact]
    public async Task An_organizer_neither_sees_nor_opens_the_game_of_another_organizer()
    {
        var game = await TestData.SeedGameAsync(app.ConnectionString, TestScenario.Conference, TestGameState.Open);
        var owner = await app.NewOrganizerPageAsync(1280, 800, ownedGames: game.Id);
        await Expect(owner.GetByRole(AriaRole.Link, new() { Name = TestScenario.Conference.GameName, Exact = true }).First).ToBeVisibleAsync();
        var other = await app.NewOrganizerPageAsync(1280, 800);
        await WaitForInteractiveAsync(other);

        await Expect(other.GetByRole(AriaRole.Link, new() { Name = TestScenario.Conference.GameName, Exact = true })).ToHaveCountAsync(0);

        await other.GotoAsync($"/organizer/games/{game.Id}");
        await WaitForInteractiveAsync(other);
        await Expect(other.GetByText("Partita non trovata.")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task A_game_is_duplicated_from_the_dashboard_into_a_new_edition()
    {
        var scenario = TestScenario.Tournament;
        var game = await TestData.SeedGameAsync(app.ConnectionString, scenario, TestGameState.Live);
        var page = await app.NewOrganizerPageAsync(1280, 800, ownedGames: game.Id);
        await WaitForInteractiveAsync(page);

        if (app.GameActionsAreForms)
        {
            // No wait for the interactive connection: the card is a native menu and the choice a plain form.
            await page.GetByText("Duplica un'edizione").ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = scenario.GameName }).ClickAsync();
        }
        else
        {
            var card = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Duplica un'edizione") });
            await ClickUntilVisibleAsync(card, page.GetByRole(AriaRole.Dialog));
            await page.GetByRole(AriaRole.Dialog).GetByRole(AriaRole.Button, new() { Name = scenario.GameName }).ClickAsync();
        }

        await page.WaitForURLAsync(new Regex(@"/organizer/games/(?!" + game.Id + ")[0-9a-f-]{36}$"));
        await page.GotoAsync("/organizer");
        await Expect(page.GetByRole(AriaRole.Link, new() { Name = $"{scenario.GameName} (copia)", Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task A_game_is_deleted_from_its_menu_after_a_confirmation()
    {
        var game = await TestData.SeedGameAsync(app.ConnectionString, TestScenario.Conference, TestGameState.Open);
        var page = await app.NewOrganizerPageAsync(1280, 800, ownedGames: game.Id);
        await WaitForInteractiveAsync(page);

        await page.Locator("details.dash-menu > summary").First.ClickAsync();
        if (app.GameActionsAreForms)
        {
            await page.GetByText("Elimina", new() { Exact = true }).ClickAsync();
            await Expect(page.GetByText("non può essere annullata")).ToBeVisibleAsync();
        }
        else
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "Elimina", Exact = true }).ClickAsync();
            await Expect(page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync();
        }

        await page.GetByRole(AriaRole.Button, new() { Name = "Sì, elimina tutto" }).ClickAsync();

        await Expect(page.GetByText("Nessuna partita qui.")).ToBeVisibleAsync();
        (await TestData.GameExistsAsync(app.ConnectionString, game.Id)).ShouldBeFalse();
    }

    [Fact]
    public async Task The_form_endpoints_leave_the_games_of_other_organizers_alone()
    {
        var game = await TestData.SeedGameAsync(app.ConnectionString, TestScenario.Conference, TestGameState.Open);
        var other = await TestData.SeedGameAsync(app.ConnectionString, TestScenario.Tournament, TestGameState.Open);
        var page = await app.NewOrganizerPageAsync(1280, 800, ownedGames: game.Id);

        var response = await page.APIRequest.PostAsync($"/organizer/actions/games/{other.Id}/delete", new() { MaxRedirects = 0 });

        response.Status.ShouldBe(302);
        (await TestData.GameExistsAsync(app.ConnectionString, other.Id)).ShouldBeTrue();
    }

    [Fact]
    public async Task Without_the_live_connection_the_page_says_so_and_the_game_menu_still_opens()
    {
        var game = await TestData.SeedGameAsync(app.ConnectionString, TestScenario.Conference, TestGameState.Open);
        var page = await app.NewOrganizerPageAsync(1280, 800, ownedGames: game.Id);
        // The connection service refuses the circuit, as it does when its quota is used up.
        await page.RouteAsync("**/_blazor/negotiate**", route => route.FulfillAsync(new() { Status = 429, Body = "Connection count reaches limit." }));
        await page.ReloadAsync();

        await Expect(page.GetByRole(AriaRole.Alert).Filter(new() { HasText = "connessione in tempo reale" })).ToBeVisibleAsync();

        // A native menu: no circuit needed to open it, and the link in it works.
        await page.Locator("details.dash-menu > summary").First.ClickAsync();
        var open = page.GetByRole(AriaRole.Link, new() { Name = "Apri", Exact = true });
        await Expect(open.First).ToBeVisibleAsync();
        await app.ScreenshotAsync(page, "organizer-game-menu");

        if (app.GameActionsAreForms)
        {
            // Duplicating is a plain form post in this mode: it works with the connection down.
            await page.GetByRole(AriaRole.Button, new() { Name = "Duplica", Exact = true }).ClickAsync();
            await page.WaitForURLAsync(new Regex(@"/organizer/games/(?!" + game.Id + ")[0-9a-f-]{36}$"));
        }
        else
        {
            await open.First.ClickAsync();
            await page.WaitForURLAsync(new Regex($"/organizer/games/{game.Id}$"));
        }
    }
}
