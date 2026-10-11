using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Shouldly;
using static Microsoft.Playwright.Assertions;

namespace Roster.E2E;

/// <summary>The public site: landing, sign in, game code, consent. Phones and laptops, light and dark.</summary>
[Collection(E2EGroup.Name)]
public sealed class PublicSiteTests(RosterAppFixture app)
{
    public static TheoryData<string, int, ColorScheme> Pages() => new()
    {
        { "/", 360, ColorScheme.Light },
        { "/", 390, ColorScheme.Dark },
        { "/", 1280, ColorScheme.Light },
        { "/login", 360, ColorScheme.Light },
        { "/login", 390, ColorScheme.Dark },
        { "/login", 1280, ColorScheme.Light },
        { "/login?error=invalid_link", 390, ColorScheme.Light },
        { "/j", 360, ColorScheme.Dark },
        { "/not-found", 390, ColorScheme.Light },
    };

    [Theory]
    [MemberData(nameof(Pages))]
    public async Task Page_fits_the_screen(string path, int width, ColorScheme scheme)
    {
        var page = await app.NewPageAsync(width, width < 1000 ? 844 : 800, scheme);

        await page.GotoAsync(path);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var overflow = await page.EvaluateAsync<int>("() => document.documentElement.scrollWidth - document.documentElement.clientWidth");
        overflow.ShouldBe(0, $"{path} overflows at {width}px");
        await app.ScreenshotAsync(page, $"public{path.Replace("/", "-").Replace("?", "-").Replace("=", "-")}-{width}-{scheme}".ToLowerInvariant(), fullPage: true);
    }

    [Fact]
    public async Task Landing_lists_the_domains_from_the_domain_packs_and_every_link_works()
    {
        var page = await app.NewPageAsync(1280, 800);
        await page.GotoAsync("/");

        // Names and descriptions come from the packs, so a new pack appears here without touching the page.
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Conferenza Tech" })).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Torneo Amatoriale" })).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Generico" })).ToBeVisibleAsync();

        var hrefs = await page.EvaluateAsync<string[]>(
            "() => [...document.querySelectorAll('a[href]')].map(a => a.getAttribute('href')).filter(h => h.startsWith('/'))");
        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = app.BaseUrl };
        foreach (var href in hrefs.Select(h => h.Split('#')[0]).Where(h => h.Length > 0).Distinct())
        {
            using var response = await http.GetAsync(new Uri(href, UriKind.Relative));
            ((int)response.StatusCode).ShouldBeLessThan(400, $"link {href} is broken");
        }
    }

    [Fact]
    public async Task Phone_menu_opens_with_the_keyboard_and_reaches_the_sign_in_page()
    {
        var page = await app.NewPageAsync(390, 844);
        await page.GotoAsync("/");

        var toggle = page.Locator("summary[aria-label=Menu]");
        await toggle.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await app.ScreenshotAsync(page, "public-phone-menu");
        await page.GetByRole(AriaRole.Link, new() { Name = "Accedi" }).Last.ClickAsync();

        await Expect(page).ToHaveURLAsync(new Regex("/login$"));
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Entra per organizzare" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Sign_in_shows_the_expired_link_state_and_the_check_your_inbox_state()
    {
        var page = await app.NewPageAsync(390, 844);

        await page.GotoAsync("/login?error=invalid_link");
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Questo link è scaduto" })).ToBeVisibleAsync();

        await page.GotoAsync("/login");
        await page.Locator("#email").FillAsync($"inbox-{Guid.NewGuid():N}@example.test");
        await page.GetByRole(AriaRole.Button, new() { Name = "Inviami il link" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Controlla la posta" })).ToBeVisibleAsync();
        await app.ScreenshotAsync(page, "public-login-sent");
    }

    [Fact]
    public async Task An_element_answers_the_consent_request_the_organizer_sent()
    {
        var game = await TestData.SeedGameAsync(app.ConnectionString, TestScenario.Conference, TestGameState.Open, consentPending: true);
        var organizer = await app.NewOrganizerPageAsync(1280, 800, ownedGames: game.Id);

        // The organizer generates the link from the elements section.
        await organizer.GotoAsync($"/organizer/games/{game.Id}/elements");
        await organizer.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await organizer.GetByRole(AriaRole.Button, new() { Name = $"Azioni per {TestScenario.Conference.Elements[0].Name}" }).ClickAsync();
        await organizer.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Genera (il|un nuovo) link di consenso") }).First.ClickAsync();
        var link = await organizer.Locator(".ds-link-field span").InnerTextAsync();
        link.ShouldContain("/consent/");

        // Whoever is in play opens it on a phone, no account needed.
        var phone = await app.NewPageAsync(390, 844);
        await phone.GotoAsync(link);
        await Expect(phone.GetByRole(AriaRole.Heading, new() { NameRegex = new Regex(Regex.Escape(TestScenario.Conference.GameName)) })).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await app.ScreenshotAsync(phone, "public-consent-phone", fullPage: true);
        await phone.GetByRole(AriaRole.Button, new() { Name = "Sì, mettimi in gioco" }).ClickAsync();
        await Expect(phone.GetByRole(AriaRole.Heading, new() { Name = "Sei in gioco." })).ToBeVisibleAsync();

        // The organizer sees the answer.
        await organizer.ReloadAsync();
        await Expect(organizer.Locator("li", new() { HasText = TestScenario.Conference.Elements[0].Name }).GetByText("Accettato")).ToBeVisibleAsync();
    }
}
