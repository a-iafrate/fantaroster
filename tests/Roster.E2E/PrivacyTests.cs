using Microsoft.Playwright;
using Shouldly;
using static Microsoft.Playwright.Assertions;

namespace Roster.E2E;

/// <summary>Terms, privacy notice and the organizer's data rights (download and delete).</summary>
[Collection(E2EGroup.Name)]
public sealed class PrivacyTests(RosterAppFixture app)
{
    [Theory]
    [InlineData("/terms", "Condizioni d'uso", 390)]
    [InlineData("/privacy", "Informativa sulla privacy", 390)]
    [InlineData("/privacy", "Informativa sulla privacy", 1280)]
    public async Task Legal_pages_are_public_localized_and_fit_the_screen(string path, string heading, int width)
    {
        var page = await app.NewPageAsync(width, width < 1000 ? 844 : 800);

        await page.GotoAsync(path);

        await Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1, Name = heading })).ToBeVisibleAsync();
        var overflow = await page.EvaluateAsync<int>("() => document.documentElement.scrollWidth - document.documentElement.clientWidth");
        overflow.ShouldBe(0);
    }

    [Fact]
    public async Task Legal_pages_follow_the_language_of_the_brand()
    {
        var page = await app.NewPageForHostAsync("imagiroster.localhost");

        await page.GotoAsync("/privacy");

        await Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1, Name = "Privacy notice" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task The_footer_and_the_sign_in_page_link_to_the_legal_pages()
    {
        var page = await app.NewPageAsync(1280, 800);

        await page.GotoAsync("/");
        await Expect(page.Locator("footer").GetByRole(AriaRole.Link, new() { Name = "Condizioni d'uso" })).ToHaveAttributeAsync("href", "/terms");
        await Expect(page.Locator("footer").GetByRole(AriaRole.Link, new() { Name = "Informativa sulla privacy" })).ToHaveAttributeAsync("href", "/privacy");

        await page.GotoAsync("/login");
        await Expect(page.Locator("main").GetByRole(AriaRole.Link, new() { Name = "Informativa sulla privacy" })).ToHaveAttributeAsync("href", "/privacy");
    }

    [Fact]
    public async Task An_organizer_downloads_their_data_and_deletes_the_account_with_their_games()
    {
        var game = await TestData.SeedGameAsync(app.ConnectionString, TestScenario.Conference, TestGameState.Open);
        var page = await app.NewOrganizerPageAsync(1280, 800, ownedGames: game.Id);
        // The account page is one click away in the side menu.
        await page.GetByRole(AriaRole.Navigation).GetByRole(AriaRole.Link, new() { Name = "Account e privacy" }).ClickAsync();
        await page.WaitForURLAsync("**/organizer/account");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var download = await page.RunAndWaitForDownloadAsync(() => page.GetByRole(AriaRole.Link, new() { Name = "Scarica i miei dati" }).ClickAsync());
        var path = await download.PathAsync();
        var json = await File.ReadAllTextAsync(path!);
        json.ShouldContain(TestScenario.Conference.GameName);
        json.ShouldNotContain("TokenHash");

        // A wrong address deletes nothing.
        await page.Locator("#account-confirm").FillAsync("someone-else@example.test");
        await page.GetByRole(AriaRole.Button, new() { Name = "Elimina il mio account e le partite" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Alert)).ToContainTextAsync("non corrisponde");
        (await TestData.GameExistsAsync(app.ConnectionString, game.Id)).ShouldBeTrue();

        // The right one deletes the account and the game, and signs out.
        var email = await TestData.GetOrganizerEmailAsync(app.ConnectionString, game.Id);
        await page.Locator("#account-confirm").FillAsync(email);
        await page.GetByRole(AriaRole.Button, new() { Name = "Elimina il mio account e le partite" }).ClickAsync();
        await page.WaitForURLAsync("**/?deleted=1");
        await Expect(page.GetByRole(AriaRole.Status)).ToContainTextAsync("eliminati");
        (await TestData.GameExistsAsync(app.ConnectionString, game.Id)).ShouldBeFalse();

        await page.GotoAsync("/organizer");
        await page.WaitForURLAsync("**/login**");
    }

    [Fact]
    public async Task The_sign_in_button_is_disabled_and_busy_once_pressed()
    {
        var page = await app.NewPageAsync(390, 844);
        // A page that is leaving cannot be queried, so a recorder in the page notes what the button looked like, and the
        // note is read from the page that follows.
        await page.AddInitScriptAsync(
            """
            new MutationObserver(() => {
                const busy = document.querySelector('button[type=submit][aria-busy=true]');
                if (busy) {
                    sessionStorage.setItem('busy', `${busy.disabled}|${busy.textContent.trim()}`);
                }
            }).observe(document, { subtree: true, attributes: true, childList: true, characterData: true });
            """);
        await page.GotoAsync("/login");
        await page.Locator("#email").FillAsync($"busy-{Guid.NewGuid():N}@example.test");
        await page.RouteAsync("**/login", async route =>
        {
            await Task.Delay(1000);
            await route.ContinueAsync();
        });

        await page.GetByRole(AriaRole.Button, new() { Name = "Inviami il link" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Controlla la posta" })).ToBeVisibleAsync();

        (await page.EvaluateAsync<string?>("() => sessionStorage.getItem('busy')")).ShouldBe("true|Invio in corso...");
    }
}
