using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Roster.E2E;

/// <summary>The public pricing page: plans, limits, Community authorization and the way to it from the rest of the site.</summary>
[Collection(E2EGroup.Name)]
public sealed class PricingTests(RosterAppFixture app)
{
    [Fact]
    public async Task The_page_shows_the_plans_with_the_configured_limits_and_the_community_steps()
    {
        var page = await app.NewPageAsync(1280, 900);

        await page.GotoAsync("/pricing");

        await Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToContainTextAsync("paga chi organizza");
        foreach (var plan in new[] { "Gratis", "Community", "Partita Premium", "Evento Pro" })
        {
            await Expect(page.GetByRole(AriaRole.Heading, new() { Level = 3, Name = plan, Exact = true })).ToBeVisibleAsync();
        }

        await Expect(page.GetByText("Fino a 50 giocatori per partita")).ToBeVisibleAsync();
        await Expect(page.GetByText("Fino a 300 giocatori per partita")).ToBeVisibleAsync();
        await Expect(page.GetByText(app.PaymentsOn ? "Paghi su PayPal" : "I prezzi sono indicativi").First).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Table).GetByRole(AriaRole.Row)).ToHaveCountAsync(5);
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "3. Ricevi una email" })).ToBeVisibleAsync();
        await app.ScreenshotAsync(page, "pricing", fullPage: true);
    }

    [Fact]
    public async Task The_questions_open_and_close_and_the_page_follows_the_brand_language()
    {
        var page = await app.NewPageForHostAsync("imagiroster.localhost", 390, 844);

        await page.GotoAsync("/pricing");

        await Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToContainTextAsync("the organizer pays");
        var question = page.GetByText("What happens when a game is full?");
        await question.ClickAsync();
        await Expect(page.GetByText("Nobody who already joined is removed")).ToBeVisibleAsync();
        await question.ClickAsync();
        await Expect(page.GetByText("Nobody who already joined is removed")).ToBeHiddenAsync();
    }

    [Fact]
    public async Task The_calls_to_action_lead_to_the_right_places_and_the_site_links_to_the_page()
    {
        var page = await app.NewPageAsync(1280, 900);

        await page.GotoAsync("/");
        await Expect(page.Locator("header").GetByRole(AriaRole.Link, new() { Name = "Prezzi" })).ToHaveAttributeAsync("href", "/pricing");
        await Expect(page.Locator("footer").GetByRole(AriaRole.Link, new() { Name = "Prezzi" })).ToHaveAttributeAsync("href", "/pricing");

        await page.GotoAsync("/pricing");
        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "Richiedi l'autorizzazione" }).First).ToHaveAttributeAsync("href", "/organizer/account#community");
        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "Crea la tua partita" }).First).ToHaveAttributeAsync("href", "/organizer/create-game");
        // The paid plans are not for sale yet: they are not links.
        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "In arrivo" })).ToHaveCountAsync(0);
    }
}
