using System.Net;
using Microsoft.Playwright;
using Shouldly;
using static Microsoft.Playwright.Assertions;

namespace Roster.E2E;

/// <summary>Security headers and a Content Security Policy that the whole app actually works under.</summary>
[Collection(E2EGroup.Name)]
public sealed class SecurityTests(RosterAppFixture app)
{
    [Theory]
    [InlineData("/")]
    [InlineData("/login")]
    [InlineData("/not-found")]
    [InlineData("/api/games/00000000-0000-0000-0000-000000000001")]
    public async Task Responses_carry_the_security_headers(string path)
    {
        using var http = new HttpClient { BaseAddress = app.BaseUrl };

        using var response = await http.GetAsync(new Uri(path, UriKind.Relative));

        var headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value), StringComparer.OrdinalIgnoreCase);
        headers["X-Content-Type-Options"].ShouldBe("nosniff");
        headers["X-Frame-Options"].ShouldBe("DENY");
        headers["Referrer-Policy"].ShouldBe("strict-origin-when-cross-origin");
        headers["Permissions-Policy"].ShouldContain("camera=()");
        var csp = headers["Content-Security-Policy"];
        csp.ShouldContain("frame-ancestors 'none'");
        csp.ShouldContain("object-src 'none'");
        csp.ShouldMatch("script-src 'self' 'nonce-[A-Za-z0-9+/=]+' 'wasm-unsafe-eval'");
        csp.ShouldNotContain("unsafe-inline");
        csp.ShouldNotContain("'unsafe-eval'", Case.Sensitive, "only wasm-unsafe-eval is allowed");
    }

    [Fact]
    public async Task The_public_pages_run_without_a_single_policy_violation()
    {
        var violations = new List<string>();
        var game = await TestData.SeedGameAsync(app.ConnectionString, TestScenario.Conference, TestGameState.Live);

        foreach (var path in new[] { "/", "/login", "/j", "/not-found", $"/games/{game.Id}/join", $"/games/{game.Id}/big-screen", $"/games/{game.Id}/referee#t={game.RefereeToken}" })
        {
            var page = await app.NewPageAsync(1280, 800);
            Watch(page, violations);
            await page.GotoAsync(path);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(1_500);
            await page.Context.CloseAsync();
        }

        violations.ShouldBeEmpty(string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public async Task The_organizer_console_and_the_participant_app_run_without_a_single_policy_violation()
    {
        var violations = new List<string>();
        var game = await TestData.SeedGameAsync(app.ConnectionString, TestScenario.Tournament, TestGameState.Open);

        var organizer = await app.NewOrganizerPageAsync(1280, 800, ownedGames: game.Id);
        Watch(organizer, violations);
        foreach (var section in new[] { "", "elements", "rules", "referees", "control", "materials", "settings" })
        {
            await organizer.GotoAsync($"/organizer/games/{game.Id}/{section}");
            await organizer.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await organizer.WaitForTimeoutAsync(800);
        }
        await organizer.GotoAsync("/organizer/create-game");
        await organizer.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var participant = await app.NewPageAsync();
        Watch(participant, violations);
        await participant.GotoAsync($"/games/{game.Id}/join");
        await participant.GetByLabel("Nickname").FillAsync("SecurityCheck");
        await participant.GetByRole(AriaRole.Button, new() { Name = "Entra nel gioco" }).ClickAsync();
        await Expect(participant.GetByRole(AriaRole.Navigation, new() { Name = "Sezioni della partita" })).ToBeVisibleAsync();
        await participant.WaitForTimeoutAsync(800);

        violations.ShouldBeEmpty(string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public async Task A_page_cannot_be_framed()
    {
        var page = await app.NewPageAsync();
        await page.SetContentAsync($"<iframe id=f src='{app.BaseUrl}login'></iframe>");
        await page.WaitForTimeoutAsync(1_500);

        var framed = await page.EvaluateAsync<bool>("() => { try { return !!document.getElementById('f').contentDocument?.querySelector('#email'); } catch { return false; } }");

        framed.ShouldBeFalse();
    }

    private static void Watch(IPage page, List<string> violations)
    {
        page.Console += (_, message) =>
        {
            if (message.Type == "error" && message.Text.Contains("Content Security Policy", StringComparison.OrdinalIgnoreCase))
            {
                lock (violations)
                {
                    violations.Add($"{page.Url}: {message.Text}");
                }
            }
        };
    }
}
