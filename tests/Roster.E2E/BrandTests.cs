using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Shouldly;
using static Microsoft.Playwright.Assertions;

namespace Roster.E2E;

/// <summary>One deployment, two brands: identity, language and PWA come from the host.</summary>
[Collection(E2EGroup.Name)]
public sealed class BrandTests(RosterAppFixture app)
{
    [Theory]
    [InlineData("fantaroster.localhost", "fantaroster", "FantaRoster", "it", "Il fanta di qualsiasi cosa")]
    [InlineData("imagiroster.localhost", "imagiroster", "ImagiRoster", "en", "Fantasy for anything")]
    public async Task The_host_decides_brand_language_and_manifest(string host, string brandId, string name, string lang, string headline)
    {
        // The browser asks for the opposite language on purpose: the brand default wins over Accept-Language.
        var page = await app.NewPageForHostAsync(host, locale: lang == "it" ? "en-US" : "it-IT");

        await page.GotoAsync("/");

        await Expect(page.Locator("html")).ToHaveAttributeAsync("data-brand", brandId);
        await Expect(page.Locator("html")).ToHaveAttributeAsync("lang", lang);
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = headline, Level = 1 })).ToBeVisibleAsync();
        await Expect(page.Locator(".pub-header .wordmark")).ToHaveAttributeAsync("aria-label", name);
        await Expect(page).ToHaveTitleAsync(new Regex(name));
        await Expect(page.Locator("meta[name=theme-color]")).ToHaveAttributeAsync("content", "#5000A5");
        await app.ScreenshotAsync(page, $"brand-{brandId}");

        // Fetched by the page itself: it resolves *.localhost like the browser does.
        var manifest = await page.EvaluateAsync<string>("() => fetch('/manifest.webmanifest').then(r => r.text())");
        using var json = JsonDocument.Parse(manifest);
        json.RootElement.GetProperty("name").GetString().ShouldBe(name);
        json.RootElement.GetProperty("lang").GetString().ShouldBe(lang);
    }

    [Fact]
    public async Task An_unknown_host_uses_the_default_brand()
    {
        var page = await app.NewPageAsync();

        await page.GotoAsync("/");

        await Expect(page.Locator("html")).ToHaveAttributeAsync("data-brand", "fantaroster");
    }

    [Fact]
    public async Task The_language_choice_is_remembered_and_beats_the_brand_default()
    {
        var page = await app.NewPageForHostAsync("imagiroster.localhost", 1280, 800);
        await page.GotoAsync("/");
        await Expect(page.Locator("html")).ToHaveAttributeAsync("lang", "en");

        await page.Locator(".pub-footer__bottom a[hreflang=it]").ClickAsync();

        await Expect(page.Locator("html")).ToHaveAttributeAsync("lang", "it");
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Il fanta di qualsiasi cosa", Level = 1 })).ToBeVisibleAsync();
        await page.ReloadAsync();
        await Expect(page.Locator("html")).ToHaveAttributeAsync("lang", "it");

        // And back: the toggle now offers the other language.
        await page.Locator(".pub-footer__bottom a[hreflang=en]").ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Fantasy for anything", Level = 1 })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task A_game_keeps_the_brand_it_was_created_under()
    {
        // Seeded games belong to FantaRoster; the big screen is opened on the other brand's host.
        var game = await TestData.SeedGameAsync(app.ConnectionString, TestScenario.Conference, TestGameState.Live);
        var page = await app.NewPageForHostAsync("imagiroster.localhost", 1920, 1080);

        await page.GotoAsync($"/games/{game.Id}/big-screen");

        await Expect(page.Locator(".stage__bar .wordmark")).ToHaveAttributeAsync("aria-label", "FantaRoster", new() { Timeout = 30_000 });
    }

    [Theory]
    [InlineData("it", "//other.example/phish", "/")]
    [InlineData("it", @"/\other.example", "/")]
    [InlineData("it", "https://other.example", "/")]
    [InlineData("en", "/games/abc/leaderboard", "/games/abc/leaderboard")]
    public async Task The_language_link_only_redirects_inside_the_site(string culture, string returnUrl, string expectedLocation)
    {
        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = app.BaseUrl };

        using var response = await http.GetAsync(new Uri($"/culture/set?culture={culture}&returnUrl={Uri.EscapeDataString(returnUrl)}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().ShouldBe(expectedLocation);
        response.Headers.GetValues("Set-Cookie").ShouldContain(c => c.StartsWith(".AspNetCore.Culture=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task An_unsupported_language_is_refused()
    {
        using var http = new HttpClient { BaseAddress = app.BaseUrl };

        using var response = await http.GetAsync(new Uri("/culture/set?culture=xx", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
