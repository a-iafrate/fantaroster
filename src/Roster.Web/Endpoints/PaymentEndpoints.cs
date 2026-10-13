using System.Text;
using Microsoft.Extensions.Localization;
using Roster.Application.Payments;
using Roster.Application.Ports;
using Roster.Application.Services;
using Roster.Domain.Payments;
using Roster.Web.Brand;
using Roster.Web.Security;

namespace Roster.Web.Endpoints;

/// <summary>
/// Buying with PayPal. The organizer is sent to PayPal with a plain form post, comes back to a return address that captures the
/// payment, and PayPal also tells us by notification, so a payment is never lost if the organizer closes the page. The price is
/// never read from the request: the form only says which option, and the server looks the price up.
/// </summary>
public static class PaymentEndpoints
{
    private const int MaxWebhookBytes = 256 * 1024;

    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var organizer = app.MapGroup(string.Empty).RequireAuthorization().WithTags("Payments");

        organizer.MapPost("/organizer/actions/games/{gameId:guid}/checkout", async (
            Guid gameId,
            HttpContext context,
            PaymentService payments,
            IStringLocalizer<SharedResource> localizer,
            IBrandAccessor brand,
            ILogger<PaymentService> logger,
            CancellationToken cancellationToken) =>
        {
            var organizerId = OrganizerIdentity.GetId(context.User);
            var form = await context.Request.ReadFormAsync(cancellationToken);
            var back = $"/organizer/games/{gameId}/plan";
            if (organizerId is null
                || !Enum.TryParse<PurchaseKind>(form["kind"].ToString(), ignoreCase: true, out var kind)
                || !Enum.IsDefined(kind)
                || !int.TryParse(form["limit"].ToString(), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var limit))
            {
                return Results.Redirect($"{back}?billing=invalid");
            }

            var baseUrl = BaseUrl(context);
            var kindName = localizer[kind == PurchaseKind.Premium ? "PricingPlanPremium" : "PricingPlanPro"].Value;
            var description = string.Format(System.Globalization.CultureInfo.CurrentCulture, localizer["PaymentOrderDescription"].Value, brand.Current.Name, kindName, limit);

            try
            {
                var result = await payments.StartPurchaseAsync(organizerId, gameId, kind, limit, description, $"{baseUrl}/billing/return/order", $"{baseUrl}/billing/cancel/order", cancellationToken);
                return result.Outcome switch
                {
                    CheckoutOutcome.Started => Results.Redirect(result.ApproveUrl!),
                    CheckoutOutcome.AlreadyCovered => Results.Redirect($"{back}?billing=covered"),
                    CheckoutOutcome.InvalidTier => Results.Redirect($"{back}?billing=invalid"),
                    CheckoutOutcome.Disabled => Results.Redirect($"{back}?billing=disabled"),
                    _ => Results.Redirect("/organizer"),
                };
            }
            catch (PaymentGatewayException ex)
            {
                PaymentLog.OrderFailed(logger, ex, gameId, ex.DebugId);
                return Results.Redirect($"{back}?billing=error");
            }
        }).WithName("CheckoutGame");

        organizer.MapGet("/billing/return/order", async (string? token, HttpContext context, PaymentService payments, ILogger<PaymentService> logger, CancellationToken cancellationToken) =>
        {
            var organizerId = OrganizerIdentity.GetId(context.User);
            if (organizerId is null || string.IsNullOrWhiteSpace(token))
            {
                return Results.Redirect("/organizer");
            }

            try
            {
                var result = await payments.CompletePurchaseAsync(token, organizerId, cancellationToken);
                if (result.GameId is null)
                {
                    return Results.Redirect("/organizer");
                }

                var flag = result.Outcome switch
                {
                    PaymentOutcome.Completed => "paid",
                    PaymentOutcome.AlreadyCompleted => "already",
                    _ => "failed",
                };
                return Results.Redirect($"/organizer/games/{result.GameId}/plan?billing={flag}");
            }
            catch (PaymentGatewayException ex)
            {
                PaymentLog.CaptureFailed(logger, ex, token, ex.DebugId);
                return Results.Redirect("/organizer");
            }
        }).WithName("ReturnFromPayPalOrder");

        organizer.MapGet("/billing/cancel/order", async (string? token, HttpContext context, PaymentService payments, CancellationToken cancellationToken) =>
        {
            var organizerId = OrganizerIdentity.GetId(context.User);
            var gameId = organizerId is null || string.IsNullOrWhiteSpace(token) ? null : await payments.CancelPurchaseAsync(token, organizerId, cancellationToken);
            return Results.Redirect(gameId is null ? "/organizer" : $"/organizer/games/{gameId}/plan?billing=cancelled");
        }).WithName("CancelPayPalOrder");

        organizer.MapPost("/organizer/account/subscribe", async (HttpContext context, PaymentService payments, ILogger<PaymentService> logger, CancellationToken cancellationToken) =>
        {
            var organizerId = OrganizerIdentity.GetId(context.User);
            if (organizerId is null)
            {
                return Results.Unauthorized();
            }

            var baseUrl = BaseUrl(context);
            try
            {
                var result = await payments.StartSubscriptionAsync(organizerId, $"{baseUrl}/billing/return/subscription", $"{baseUrl}/billing/cancel/subscription", cancellationToken);
                return result.Outcome switch
                {
                    CheckoutOutcome.Started => Results.Redirect(result.ApproveUrl!),
                    CheckoutOutcome.NotEligible => Results.Redirect("/organizer/account?billing=noteligible#community"),
                    CheckoutOutcome.AlreadyCovered => Results.Redirect("/organizer/account?billing=active#community"),
                    _ => Results.Redirect("/organizer/account?billing=error#community"),
                };
            }
            catch (PaymentGatewayException ex)
            {
                PaymentLog.SubscriptionCreateFailed(logger, ex, ex.DebugId);
                return Results.Redirect("/organizer/account?billing=error#community");
            }
        }).WithName("SubscribeCommunity");

        organizer.MapGet("/billing/return/subscription", async (string? subscription_id, HttpContext context, PaymentService payments, ILogger<PaymentService> logger, CancellationToken cancellationToken) =>
        {
            var organizerId = OrganizerIdentity.GetId(context.User);
            if (organizerId is null || string.IsNullOrWhiteSpace(subscription_id))
            {
                return Results.Redirect("/organizer/account#community");
            }

            try
            {
                var result = await payments.CompleteSubscriptionAsync(subscription_id, organizerId, cancellationToken);
                var flag = result.Outcome is PaymentOutcome.Completed or PaymentOutcome.AlreadyCompleted ? "active" : "pending";
                return Results.Redirect($"/organizer/account?billing={flag}#community");
            }
            catch (PaymentGatewayException ex)
            {
                PaymentLog.SubscriptionReadFailed(logger, ex, subscription_id, ex.DebugId);
                return Results.Redirect("/organizer/account?billing=error#community");
            }
        }).WithName("ReturnFromPayPalSubscription");

        organizer.MapGet("/billing/cancel/subscription", () => Results.Redirect("/organizer/account?billing=notcompleted#community")).WithName("CancelPayPalSubscription");

        organizer.MapPost("/organizer/account/subscription/cancel", async (HttpContext context, PaymentService payments, ILogger<PaymentService> logger, CancellationToken cancellationToken) =>
        {
            var organizerId = OrganizerIdentity.GetId(context.User);
            if (organizerId is null)
            {
                return Results.Unauthorized();
            }

            try
            {
                var done = await payments.CancelSubscriptionAsync(organizerId, "Cancelled by the organizer", cancellationToken);
                return Results.Redirect($"/organizer/account?billing={(done ? "ended" : "error")}#community");
            }
            catch (PaymentGatewayException ex)
            {
                PaymentLog.SubscriptionCancelFailed(logger, ex, ex.DebugId);
                return Results.Redirect("/organizer/account?billing=error#community");
            }
        }).WithName("CancelCommunitySubscription");

        // Notifications from PayPal: no sign-in, the signature is what proves who sends them.
        app.MapPost("/api/payments/paypal/webhook", async (HttpContext context, PaymentSettings settings, IPaymentGateway gateway, PaymentService payments, ILogger<PaymentService> logger, CancellationToken cancellationToken) =>
        {
            if (!settings.PurchasesEnabled)
            {
                return Results.NotFound();
            }

            if (context.Request.ContentLength is > MaxWebhookBytes)
            {
                return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
            }

            string body;
            using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
            {
                body = await reader.ReadToEndAsync(cancellationToken);
            }

            var headers = context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!await gateway.VerifyWebhookAsync(new WebhookRequest(headers, body), cancellationToken))
                {
                    return Results.BadRequest();
                }

                if (gateway.ParseWebhook(body) is { } notification)
                {
                    await payments.HandleWebhookAsync(notification, cancellationToken);
                }

                // Answered 200 even for notifications we do not use, so PayPal does not keep sending them.
                return Results.Ok();
            }
            catch (PaymentGatewayException ex)
            {
                PaymentLog.WebhookCheckFailed(logger, ex, ex.DebugId);
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }).AllowAnonymous().WithName("PayPalWebhook");

        app.MapPost("/admin/actions/payments/community-plan", async (PaymentSettings settings, PaymentService payments, ILogger<PaymentService> logger, CancellationToken cancellationToken) =>
        {
            if (!settings.PurchasesEnabled)
            {
                return Results.Redirect("/admin/payments?plan=off");
            }

            try
            {
                var planId = await payments.CreateCommunityPlanAsync(cancellationToken);
                return Results.Redirect($"/admin/payments?plan={Uri.EscapeDataString(planId)}");
            }
            catch (PaymentGatewayException ex)
            {
                PaymentLog.PlanCreateFailed(logger, ex, ex.DebugId);
                return Results.Redirect("/admin/payments?plan=error");
            }
        }).RequireAuthorization(AdminAccess.PolicyName).WithName("CreateCommunityPlan");

        return app;
    }

    private static string BaseUrl(HttpContext context) => $"{context.Request.Scheme}://{context.Request.Host}";
}
