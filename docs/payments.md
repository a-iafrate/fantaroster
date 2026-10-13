# Payments with PayPal

How paying for a plan works, how to switch it on, and what to check before it goes live.

## What can be bought

| What | How it is paid | What it gives |
|---|---|---|
| **Premium game** and **Pro event** | One payment per game (PayPal Orders v2: create, approve, capture) | The participant limit of that game rises to the tier that was bought |
| **Community plan** | A yearly subscription (PayPal Subscriptions) | The community limit for every game of the organizer, as long as the subscription is paid |

Rules that never change:

- **Payments are off unless PayPal is configured.** Without the settings below nothing can be bought, the plans page says "coming soon", and the Community limit follows the approval alone, as before payments existed.
- **The price is decided by the server.** The page only says which tier; the amount comes from the price list and is checked again when the money is captured (a different amount grants nothing).
- **The Community plan is only for organizers an administrator approved.** The subscription is refused for anybody else, even if its address is posted by hand.
- **An administrator limit on a single game always wins** over the plan and over any purchase.
- **A payment is never lost or counted twice.** The organizer comes back from PayPal to a return address that captures the money, and PayPal also sends a notification (webhook); whichever arrives first grants the room and the other changes nothing. Notifications are checked with PayPal signature verification and recorded by id.
- **A refund takes the room away again**, keeping what the other payments of the game bought.

## Settings

Put the secrets in user secrets locally and in Key Vault / container app secrets on Azure, never in a file that is committed.

| Setting | Meaning |
|---|---|
| `Payments:PayPal:Mode` | `Sandbox` (default) or `Live`. `Fake` is a built-in stand-in for tests and local work and only works in Development. |
| `Payments:PayPal:ClientId`, `Payments:PayPal:ClientSecret` | The REST app credentials. **Both are needed to sell anything.** |
| `Payments:PayPal:WebhookId` | The id of the webhook registered in the PayPal dashboard. |
| `Payments:PayPal:CommunityPlanId` | The id of the yearly billing plan (see below). |
| `Payments:PayPal:BrandName` | The name PayPal shows to the buyer. |
| `Payments:Currency` | ISO code, default `EUR`. |
| `Payments:PremiumTiers`, `Payments:ProTiers` | Price lists: `[{ "ParticipantLimit": 100, "Price": 3 }, ...]`. Defaults: Premium 100 / 200 / 300 players at 3 / 5 / 9, Pro 500 / 1000 / 2000 players at 99 / 199 / 299 (indicative: to be validated with the pilot). |
| `Payments:CommunityYearlyPrice` | Default `10`. |

What is switched on depends on what is configured:

- credentials present: single-game purchases;
- credentials, `CommunityPlanId` **and** `WebhookId` present: the Community subscription too (renewals and cancellations only reach the application through webhooks, so it is not offered without them). From then on the Community limit needs an approved request **and** a paid subscription.

Environment variables use double underscores: `Payments__PayPal__ClientId`.

## Setting up PayPal

1. In the PayPal developer dashboard create a **REST app** (Sandbox first). Copy its client id and secret into the settings.
2. Register a **webhook** for `https://<your host>/api/payments/paypal/webhook` with these events: `PAYMENT.CAPTURE.COMPLETED`, `PAYMENT.CAPTURE.REFUNDED`, `PAYMENT.CAPTURE.REVERSED`, `PAYMENT.CAPTURE.DENIED`, `BILLING.SUBSCRIPTION.ACTIVATED`, `BILLING.SUBSCRIPTION.CANCELLED`, `BILLING.SUBSCRIPTION.SUSPENDED`, `BILLING.SUBSCRIPTION.EXPIRED`, `BILLING.SUBSCRIPTION.PAYMENT.FAILED`, `PAYMENT.SALE.COMPLETED`. Copy the webhook id into `Payments:PayPal:WebhookId`. Use one webhook per host (brand) if the brands have different hosts.
3. Start the app with the credentials, sign in as an administrator and open **Administration > Payments**: press **Create the Community plan on PayPal**. It creates the product and the yearly plan and shows the plan id; put it in `Payments:PayPal:CommunityPlanId` and restart.
4. Try a purchase and a subscription with a Sandbox buyer account, then check the Payments page.
5. For production repeat the steps with `Mode = Live`, the live credentials and the live webhook.

## Where it lives

- `Roster.Domain/Payments`: `Purchase`, `Subscription`, `PaymentEvent`; `Game.ApplyPurchase`, `Game.EffectiveParticipantLimit`.
- `Roster.Application`: `PaymentService` (all the rules above), `PlanService`, the `IPaymentGateway` port.
- `Roster.Infrastructure/Payments`: `PayPalGateway` (REST, typed `HttpClient` with the standard resilience handler, a request id on every creation so a retry cannot pay twice, the access token shared and cached), `PayPalWebhookParser`, `FakePaymentGateway`, `DisabledPaymentGateway`.
- `Roster.Web`: the endpoints (`/organizer/actions/games/{id}/checkout`, `/billing/return/...`, `/billing/cancel/...`, `/organizer/account/subscribe`, `/api/payments/paypal/webhook`), the **Plan** tab of a game, the Community section of the account page, the pricing page and **Administration > Payments**.
- The content security policy lets a form post be answered by a redirect to `www.paypal.com` and `www.sandbox.paypal.com` (`form-action`).

## Tests

- Unit: `PaymentsTests` (domain), `PaymentServiceTests` (every rule above, with a substituted gateway).
- `PayPalGatewayTests` and `PayPalWebhookParserTests` run the real gateway against canned PayPal answers: no network.
- `PaymentEndpointsTests` posts signed and forged notifications to the real endpoint and database.
- `PaymentTests` (end to end) buy, repeat, cancel and subscribe through the stand-in. `ROSTER_E2E_PAYMENTS=Off` runs the suite with payments off.

## Not covered yet

- The unlock code that makes an organizer eligible for the Community plan (today eligibility is the approval of an administrator).
- Invoices and VAT: PayPal sends its own receipts; fiscal documents are a separate decision.
- Refunds are made from the PayPal dashboard; the notification then takes the room away.
- Disputes beyond `PAYMENT.CAPTURE.REVERSED`.
