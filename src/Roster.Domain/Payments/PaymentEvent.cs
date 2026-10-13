namespace Roster.Domain.Payments;

/// <summary>A notification from the payment provider that was already handled, so a repeated delivery does nothing.</summary>
public sealed class PaymentEvent
{
    /// <summary>The id the provider gave to the notification.</summary>
    public string Id { get; private set; }

    public string Type { get; private set; }

    public DateTimeOffset ReceivedAt { get; private set; }

    private PaymentEvent()
    {
        Id = string.Empty;
        Type = string.Empty;
    }

    public PaymentEvent(string id, string type, DateTimeOffset receivedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);

        Id = id;
        Type = type;
        ReceivedAt = receivedAt;
    }
}
