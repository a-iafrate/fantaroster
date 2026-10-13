using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Shouldly;
using static Microsoft.Playwright.Assertions;

namespace Roster.E2E;

/// <summary>Buying a plan: a one-time purchase for a game and the yearly Community subscription (PayPal is the stand-in).</summary>
[Collection(E2EGroup.Name)]
public sealed class PaymentTests(RosterAppFixture app)
{
    private async Task<(IPage Page, SeededGame Game)> OrganizerWithGameAsync()
    {
        var game = await TestData.SeedGameAsync(app.ConnectionString, TestScenario.Conference, TestGameState.Open);
        var page = await app.NewOrganizerPageAsync(1280, 900, ownedGames: game.Id);
        return (page, game);
    }

    [Fact]
    public async Task A_game_is_upgraded_with_a_purchase_and_the_organizer_gets_a_receipt()
    {
        // These journeys need payments on; in a run with payments off (ROSTER_E2E_PAYMENTS=Off) they have nothing to check.
        if (!app.PaymentsOn)
        {
            return;
        }

        var (page, game) = await OrganizerWithGameAsync();

        await page.GotoAsync($"/organizer/games/{game.Id}/plan");
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Questa partita accetta fino a 50 giocatori." })).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Acquista a 5,00 EUR" }).ClickAsync();

        await page.WaitForURLAsync(new Regex($"/organizer/games/{game.Id}/plan\\?billing=paid"));
        await Expect(page.GetByText("Pagamento ricevuto")).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Questa partita accetta fino a 200 giocatori." })).ToBeVisibleAsync();
        await Expect(page.GetByText("Hai acquistato il piano Premium")).ToBeVisibleAsync();
        (await TestData.GetPurchaseStatusAsync(app.ConnectionString, game.Id)).ShouldBe("Completed");
        // The tiers up to the bought one are included, the next ones can still be bought.
        await Expect(page.GetByText("Già incluso")).ToHaveCountAsync(2);
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Acquista a 9,00 EUR" })).ToBeVisibleAsync();
        (app.ServerLogContains("Thank you for your payment") || app.ServerLogContains("Grazie per il tuo pagamento")).ShouldBeTrue();
        await app.ScreenshotAsync(page, "plan-after-purchase", fullPage: true);
    }

    /// <summary>Starts a purchase the way the page form does, and returns where the browser is sent next (the stand-in approves at once).</summary>
    private static async Task<string> StartCheckoutAsync(IPage page, SeededGame game, int limit)
    {
        var response = await page.APIRequest.PostAsync(
            $"/organizer/actions/games/{game.Id}/checkout",
            new() { MaxRedirects = 0, Form = page.APIRequest.CreateFormData().Set("kind", "premium").Set("limit", limit.ToString(System.Globalization.CultureInfo.InvariantCulture)) });
        response.Status.ShouldBe(302);
        return response.Headers["location"];
    }

    [Fact]
    public async Task Coming_back_twice_from_PayPal_counts_the_payment_once()
    {
        // These journeys need payments on; in a run with payments off (ROSTER_E2E_PAYMENTS=Off) they have nothing to check.
        if (!app.PaymentsOn)
        {
            return;
        }

        var (page, game) = await OrganizerWithGameAsync();
        var returnUrl = await StartCheckoutAsync(page, game, 100);

        await page.GotoAsync(returnUrl);
        await page.WaitForURLAsync(new Regex("billing=paid"));
        await page.GotoAsync(returnUrl);

        await page.WaitForURLAsync(new Regex("billing=already"));
        await Expect(page.GetByText("già stato conteggiato")).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Questa partita accetta fino a 100 giocatori." })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Cancelling_at_PayPal_charges_nothing_and_changes_nothing()
    {
        if (!app.PaymentsOn)
        {
            return;
        }

        var (page, game) = await OrganizerWithGameAsync();
        // The person presses "cancel" at PayPal: it sends them to the cancel address instead of the return one.
        var cancelUrl = (await StartCheckoutAsync(page, game, 100)).Replace("/billing/return/order", "/billing/cancel/order", StringComparison.Ordinal);

        await page.GotoAsync(cancelUrl);

        await page.WaitForURLAsync(new Regex("billing=cancelled"));
        await Expect(page.GetByText("Non è stato addebitato nulla")).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Questa partita accetta fino a 50 giocatori." })).ToBeVisibleAsync();
        (await TestData.GetPurchaseStatusAsync(app.ConnectionString, game.Id)).ShouldBe("Cancelled");
    }

    [Fact]
    public async Task A_price_chosen_by_hand_is_not_accepted()
    {
        // These journeys need payments on; in a run with payments off (ROSTER_E2E_PAYMENTS=Off) they have nothing to check.
        if (!app.PaymentsOn)
        {
            return;
        }

        var (page, game) = await OrganizerWithGameAsync();

        var response = await page.APIRequest.PostAsync(
            $"/organizer/actions/games/{game.Id}/checkout",
            new() { MaxRedirects = 0, Form = page.APIRequest.CreateFormData().Set("kind", "premium").Set("limit", "150") });

        response.Status.ShouldBe(302);
        response.Headers["location"].ShouldContain("billing=invalid");
        (await TestData.GetPurchaseStatusAsync(app.ConnectionString, game.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task Nobody_buys_for_the_game_of_somebody_else()
    {
        // These journeys need payments on; in a run with payments off (ROSTER_E2E_PAYMENTS=Off) they have nothing to check.
        if (!app.PaymentsOn)
        {
            return;
        }

        var game = await TestData.SeedGameAsync(app.ConnectionString, TestScenario.Tournament, TestGameState.Open);
        var other = await app.NewOrganizerPageAsync(1280, 900);

        var response = await other.APIRequest.PostAsync(
            $"/organizer/actions/games/{game.Id}/checkout",
            new() { MaxRedirects = 0, Form = other.APIRequest.CreateFormData().Set("kind", "premium").Set("limit", "200") });

        response.Status.ShouldBe(302);
        response.Headers["location"].ShouldNotContain("paypal");
        (await TestData.GetPurchaseStatusAsync(app.ConnectionString, game.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task The_Community_plan_needs_the_approval_then_is_subscribed_and_can_be_stopped()
    {
        // These journeys need payments on; in a run with payments off (ROSTER_E2E_PAYMENTS=Off) they have nothing to check.
        if (!app.PaymentsOn)
        {
            return;
        }

        var game = await TestData.SeedGameAsync(app.ConnectionString, TestScenario.Conference, TestGameState.Open);
        var organizer = await app.NewOrganizerPageAsync(1280, 900, ownedGames: game.Id);

        // Not approved: the subscription is refused even when its address is posted by hand.
        var refused = await organizer.APIRequest.PostAsync("/organizer/account/subscribe", new() { MaxRedirects = 0 });
        refused.Status.ShouldBe(302);
        refused.Headers["location"].ShouldContain("billing=noteligible");

        await organizer.GotoAsync("/organizer/account");
        await organizer.Locator("#community-description").FillAsync("Il gruppo di utenti di Bologna, sei incontri all'anno.");
        await organizer.GetByRole(AriaRole.Button, new() { Name = "Invia la richiesta" }).ClickAsync();
        var (admin, _) = await app.NewAdminPageAsync();
        await admin.GotoAsync("/admin/requests");
        var request = admin.GetByRole(AriaRole.Listitem).Filter(new() { HasText = "sei incontri" });
        await request.GetByRole(AriaRole.Button, new() { Name = "Approva" }).ClickAsync();
        await Expect(admin.GetByText("Decisione salvata")).ToBeVisibleAsync();

        // Approved but not paid yet: the higher limit is still off.
        await organizer.GotoAsync("/organizer/account");
        await Expect(organizer.GetByText("Abbiamo approvato la tua community")).ToBeVisibleAsync();
        await organizer.GotoAsync($"/organizer/games/{game.Id}/plan");
        await Expect(organizer.GetByRole(AriaRole.Heading, new() { Name = "Questa partita accetta fino a 50 giocatori." })).ToBeVisibleAsync();

        await organizer.GotoAsync("/organizer/account");
        await organizer.GetByRole(AriaRole.Button, new() { Name = "Abbonati con PayPal" }).ClickAsync();
        await organizer.WaitForURLAsync(new Regex("billing=active"));
        await Expect(organizer.GetByText("Abbonamento attivo")).ToBeVisibleAsync();
        await Expect(organizer.GetByText("Il tuo piano Community è attivo fino al")).ToBeVisibleAsync();

        await organizer.GotoAsync($"/organizer/games/{game.Id}/plan");
        await Expect(organizer.GetByRole(AriaRole.Heading, new() { Name = "Questa partita accetta fino a 300 giocatori." })).ToBeVisibleAsync();

        await organizer.GotoAsync("/organizer/account");
        await organizer.GetByRole(AriaRole.Button, new() { Name = "Interrompi il rinnovo" }).ClickAsync();
        await organizer.WaitForURLAsync(new Regex("billing=ended"));
        await Expect(organizer.GetByText("Rinnovo interrotto. Il tuo piano Community resta attivo fino al")).ToBeVisibleAsync();

        // The paid period is kept after the renewal stops.
        await organizer.GotoAsync($"/organizer/games/{game.Id}/plan");
        await Expect(organizer.GetByRole(AriaRole.Heading, new() { Name = "Questa partita accetta fino a 300 giocatori." })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task The_administrator_sees_the_payments_and_the_pricing_page_says_the_plans_are_on_sale()
    {
        // These journeys need payments on; in a run with payments off (ROSTER_E2E_PAYMENTS=Off) they have nothing to check.
        if (!app.PaymentsOn)
        {
            return;
        }

        var (page, game) = await OrganizerWithGameAsync();
        await page.GotoAsync($"/organizer/games/{game.Id}/plan");
        await page.GetByRole(AriaRole.Button, new() { Name = "Acquista a 5,00 EUR" }).ClickAsync();
        await page.WaitForURLAsync(new Regex("billing=paid"));

        var (admin, _) = await app.NewAdminPageAsync();
        await admin.GotoAsync("/admin/payments");
        await Expect(admin.GetByRole(AriaRole.Heading, new() { Level = 1, Name = "Pagamenti" })).ToBeVisibleAsync();
        await Expect(admin.GetByRole(AriaRole.Cell, new() { Name = "5,00 EUR" }).First).ToBeVisibleAsync();

        var visitor = await app.NewPageAsync(1280, 900);
        await visitor.GotoAsync("/pricing");
        await Expect(visitor.GetByText("Fino a 200 giocatori: 5,00 EUR")).ToBeVisibleAsync();
        await Expect(visitor.GetByRole(AriaRole.Link, new() { Name = "Scegli la partita da potenziare" })).ToHaveCountAsync(2);
        await Expect(visitor.GetByText("In arrivo")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task With_payments_off_nothing_can_be_bought()
    {
        if (app.PaymentsOn)
        {
            return;
        }

        var (page, game) = await OrganizerWithGameAsync();

        await page.GotoAsync($"/organizer/games/{game.Id}/plan");
        await Expect(page.GetByText("I piani a pagamento non sono ancora aperti")).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Acquista") })).ToHaveCountAsync(0);

        var visitor = await app.NewPageAsync(1280, 900);
        await visitor.GotoAsync("/pricing");
        await Expect(visitor.GetByText("In arrivo").First).ToBeVisibleAsync();
    }
}
