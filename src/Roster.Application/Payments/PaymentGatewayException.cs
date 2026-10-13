namespace Roster.Application.Payments;

/// <summary>The payment provider refused a request or could not be reached.</summary>
public sealed class PaymentGatewayException : Exception
{
    /// <summary>The identifier the provider gave to the failed call, to quote when asking its support (never shown to people).</summary>
    public string? DebugId { get; }

    public PaymentGatewayException(string message, string? debugId = null, Exception? inner = null)
        : base(message, inner)
    {
        DebugId = debugId;
    }
}
