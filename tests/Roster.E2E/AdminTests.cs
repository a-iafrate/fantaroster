using Microsoft.Playwright;
using Shouldly;
using static Microsoft.Playwright.Assertions;

namespace Roster.E2E;

/// <summary>The internal administration: community requests, per-game participant limits and statistics.</summary>
[Collection(E2EGroup.Name)]
public sealed class AdminTests(RosterAppFixture app)
{
    private static async Task JoinAsync(IPage page, string joinCode, string nickname)
    {
        await page.GotoAsync("/j");
        await page.GetByLabel("Codice partita").FillAsync(joinCode);
        await page.GetByRole(AriaRole.Button, new() { Name = "Vai al gioco" }).ClickAsync();
        await page.GetByLabel("Nickname").FillAsync(nickname);
        await page.GetByRole(AriaRole.Button, new() { Name = "Entra nel gioco" }).ClickAsync();
    }

    [Fact]
    public async Task An_organizer_who_is_not_an_administrator_finds_no_admin_pages()
    {
        var page = await app.NewOrganizerPageAsync(1280, 800);

        await page.GotoAsync("/admin/games");

        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Pagina non trovata" })).ToBeVisibleAsync();
        await page.GotoAsync("/organizer");
        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "Statistiche" })).ToHaveCountAsync(0);

        // The actions are closed to them too, even when posted by hand.
        var response = await page.APIRequest.PostAsync($"/admin/actions/games/{Guid.NewGuid()}/limit", new() { MaxRedirects = 0, Form = page.APIRequest.CreateFormData() });
        response.Status.ShouldBe(302);
        response.Headers["location"].ShouldContain("/not-found");
    }

    [Fact]
    public async Task The_administrator_sees_the_statistics_of_the_service()
    {
        var game = await TestData.SeedGameAsync(app.ConnectionString, TestScenario.Conference, TestGameState.Live);
        await TestData.AddScoresAsync(app.BaseUrl, game);
        var (page, _) = await app.NewAdminPageAsync();

        await page.GotoAsync("/admin");

        await Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1, Name = "Amministrazione" })).ToBeVisibleAsync();
        await Expect(page.GetByText("Nuovi giocatori, ultimi 7 giorni")).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Partite con più giocatori" })).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "Richieste community" })).ToBeVisibleAsync();
        await app.ScreenshotAsync(page, "admin-dashboard", fullPage: true);
    }

    [Fact]
    public async Task A_community_request_is_approved_and_the_organizer_is_told_by_email()
    {
        var organizer = await app.NewOrganizerPageAsync(1280, 800);
        await organizer.GotoAsync("/organizer/account");
        await organizer.Locator("#community-description").FillAsync("Il gruppo di utenti .NET di Bologna, otto incontri all'anno.");
        await organizer.Locator("#community-link").FillAsync("https://example.test/dotnet-bologna");
        await organizer.GetByRole(AriaRole.Button, new() { Name = "Invia la richiesta" }).ClickAsync();
        await Expect(organizer.GetByText("Richiesta inviata")).ToBeVisibleAsync();
        await Expect(organizer.GetByText("La tua richiesta è in verifica")).ToBeVisibleAsync();

        var (admin, _) = await app.NewAdminPageAsync();
        await admin.GotoAsync("/admin/requests");
        var request = admin.GetByRole(AriaRole.Listitem).Filter(new() { HasText = "dotnet-bologna" });
        await request.Locator("textarea").FillAsync("Benvenuti!");
        await request.GetByRole(AriaRole.Button, new() { Name = "Approva" }).ClickAsync();
        await Expect(admin.GetByText("Decisione salvata")).ToBeVisibleAsync();

        await organizer.GotoAsync("/organizer/account");
        await Expect(organizer.GetByText("Sei una community riconosciuta")).ToBeVisibleAsync();
        await Expect(organizer.Locator("#community-description")).ToHaveCountAsync(0);

        // The development email sender writes the message to the log of the server.
        (app.ServerLogContains("community request on") || app.ServerLogContains("richiesta community su")).ShouldBeTrue();
    }

    [Fact]
    public async Task A_rejected_request_can_be_sent_again()
    {
        var organizer = await app.NewOrganizerPageAsync(1280, 800);
        await organizer.GotoAsync("/organizer/account");
        await organizer.Locator("#community-description").FillAsync("Una festa tra amici, nulla di più, ma vorrei più posti.");
        await organizer.GetByRole(AriaRole.Button, new() { Name = "Invia la richiesta" }).ClickAsync();
        await Expect(organizer.GetByText("Richiesta inviata")).ToBeVisibleAsync();

        var (admin, _) = await app.NewAdminPageAsync();
        await admin.GotoAsync("/admin/requests");
        var request = admin.GetByRole(AriaRole.Listitem).Filter(new() { HasText = "nulla di più" });
        await request.GetByRole(AriaRole.Button, new() { Name = "Rifiuta" }).ClickAsync();
        await Expect(admin.GetByText("Decisione salvata")).ToBeVisibleAsync();

        await organizer.GotoAsync("/organizer/account");
        await Expect(organizer.GetByText("non è stata approvata")).ToBeVisibleAsync();
        await Expect(organizer.Locator("#community-description")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task The_administrator_raises_the_limit_of_one_game_and_a_full_game_accepts_players_again()
    {
        // The seeded game already has three players.
        var game = await TestData.SeedGameAsync(app.ConnectionString, TestScenario.Tournament, TestGameState.Open);
        var (admin, _) = await app.NewAdminPageAsync();
        await admin.GotoAsync("/admin/games");

        await admin.Locator($"#limit-{game.Id}").FillAsync("3");
        await admin.Locator($"#label-{game.Id}").FillAsync("Evento di lancio");
        await admin.Locator($"#limit-{game.Id}").PressAsync("Enter");
        await Expect(admin.GetByText("Limite salvato")).ToBeVisibleAsync();
        await Expect(admin.GetByText("Evento di lancio").First).ToBeVisibleAsync();

        var player = await app.NewPageAsync();
        await JoinAsync(player, game.JoinCode, "UnoDiTroppo");
        await Expect(player.GetByText("al completo")).ToBeVisibleAsync();

        await admin.GotoAsync("/admin/games");
        await admin.Locator($"#limit-{game.Id}").FillAsync("4");
        await admin.Locator($"#limit-{game.Id}").PressAsync("Enter");
        await Expect(admin.GetByText("Limite salvato")).ToBeVisibleAsync();

        await JoinAsync(player, game.JoinCode, "UnoDiTroppo");
        await Expect(player).ToHaveURLAsync(new System.Text.RegularExpressions.Regex($"/games/{game.Id}/lineup$"));
    }

    [Fact]
    public async Task An_invalid_limit_is_refused()
    {
        var game = await TestData.SeedGameAsync(app.ConnectionString, TestScenario.Tournament, TestGameState.Open);
        var (admin, _) = await app.NewAdminPageAsync();
        await admin.GotoAsync("/admin/games");

        await admin.Locator($"#limit-{game.Id}").EvaluateAsync("e => { e.removeAttribute('min'); e.removeAttribute('max'); }");
        await admin.Locator($"#limit-{game.Id}").FillAsync("0");
        await admin.Locator($"#limit-{game.Id}").PressAsync("Enter");

        await Expect(admin.GetByRole(AriaRole.Alert)).ToContainTextAsync("Non è stato possibile salvare");
    }
}
