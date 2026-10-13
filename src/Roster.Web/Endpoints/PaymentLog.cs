namespace Roster.Web.Endpoints;

/// <summary>The log messages of the payment endpoints. They carry the PayPal debug id, to quote when asking its support.</summary>
internal static partial class PaymentLog
{
    [LoggerMessage(LogLevel.Error, "PayPal refused to create the order for game {GameId} (debug id {DebugId})")]
    public static partial void OrderFailed(ILogger logger, Exception exception, Guid gameId, string? debugId);

    [LoggerMessage(LogLevel.Error, "PayPal failed while capturing order {OrderId} (debug id {DebugId})")]
    public static partial void CaptureFailed(ILogger logger, Exception exception, string orderId, string? debugId);

    [LoggerMessage(LogLevel.Error, "PayPal refused to create the subscription (debug id {DebugId})")]
    public static partial void SubscriptionCreateFailed(ILogger logger, Exception exception, string? debugId);

    [LoggerMessage(LogLevel.Error, "PayPal failed while reading subscription {SubscriptionId} (debug id {DebugId})")]
    public static partial void SubscriptionReadFailed(ILogger logger, Exception exception, string subscriptionId, string? debugId);

    [LoggerMessage(LogLevel.Error, "PayPal refused to cancel the subscription (debug id {DebugId})")]
    public static partial void SubscriptionCancelFailed(ILogger logger, Exception exception, string? debugId);

    [LoggerMessage(LogLevel.Error, "A PayPal notification could not be checked (debug id {DebugId})")]
    public static partial void WebhookCheckFailed(ILogger logger, Exception exception, string? debugId);

    [LoggerMessage(LogLevel.Error, "PayPal refused to create the Community plan (debug id {DebugId})")]
    public static partial void PlanCreateFailed(ILogger logger, Exception exception, string? debugId);
}
