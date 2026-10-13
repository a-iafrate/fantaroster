namespace Roster.Domain.Payments;

/// <summary>
/// A one-time payment that raises the participant limit of one game. The amount and the limit are decided by the server
/// when the order is created and are checked again when the payment is captured.
/// </summary>
public sealed class Purchase
{
    public Guid Id { get; private set; }

    public string OrganizerId { get; private set; }

    public Guid GameId { get; private set; }

    public PurchaseKind Kind { get; private set; }

    /// <summary>The participant limit the payment buys.</summary>
    public int ParticipantLimit { get; private set; }

    public decimal Amount { get; private set; }

    /// <summary>ISO 4217 code, e.g. EUR.</summary>
    public string Currency { get; private set; }

    public PurchaseStatus Status { get; private set; }

    /// <summary>The id of the order at the payment provider.</summary>
    public string ProviderOrderId { get; private set; }

    public string? ProviderCaptureId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    private Purchase()
    {
        OrganizerId = string.Empty;
        Currency = string.Empty;
        ProviderOrderId = string.Empty;
    }

    public Purchase(Guid id, string organizerId, Guid gameId, PurchaseKind kind, int participantLimit, decimal amount, string currency, string providerOrderId, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerOrderId);
        ArgumentOutOfRangeException.ThrowIfLessThan(participantLimit, 1);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(amount, 0m);

        Id = id;
        OrganizerId = organizerId;
        GameId = gameId;
        Kind = kind;
        ParticipantLimit = participantLimit;
        Amount = amount;
        Currency = currency.ToUpperInvariant();
        ProviderOrderId = providerOrderId;
        Status = PurchaseStatus.Created;
        CreatedAt = createdAt;
    }

    public void Complete(string captureId, DateTimeOffset when)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(captureId);
        if (Status is PurchaseStatus.Completed)
        {
            return;
        }

        if (Status is not (PurchaseStatus.Created or PurchaseStatus.Failed))
        {
            throw new InvalidOperationException($"A {Status} purchase cannot be completed.");
        }

        Status = PurchaseStatus.Completed;
        ProviderCaptureId = captureId;
        CompletedAt = when;
    }

    public void Fail()
    {
        if (Status == PurchaseStatus.Created)
        {
            Status = PurchaseStatus.Failed;
        }
    }

    public void Cancel()
    {
        if (Status == PurchaseStatus.Created)
        {
            Status = PurchaseStatus.Cancelled;
        }
    }

    public void Refund()
    {
        if (Status != PurchaseStatus.Completed)
        {
            throw new InvalidOperationException("Only a completed purchase can be refunded.");
        }

        Status = PurchaseStatus.Refunded;
    }
}
