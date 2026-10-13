using Roster.Domain.Payments;

namespace Roster.Application.Ports;

/// <summary>Tells the organizer, by email, about a payment that went through.</summary>
public interface IPaymentNotifier
{
    /// <summary>A receipt for a one-time payment. A failure to send must not fail the payment: implementations log it.</summary>
    Task NotifyPurchaseCompletedAsync(Purchase purchase, string gameName, CancellationToken cancellationToken = default);

    Task NotifySubscriptionActivatedAsync(Subscription subscription, CancellationToken cancellationToken = default);
}
