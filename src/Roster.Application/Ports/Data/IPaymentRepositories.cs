using Roster.Domain.Payments;

namespace Roster.Application.Ports.Data;

public interface IPurchaseRepository
{
    void Add(Purchase purchase);

    Task<Purchase?> GetByOrderIdAsync(string providerOrderId, CancellationToken cancellationToken = default);

    Task<Purchase?> GetByCaptureIdAsync(string providerCaptureId, CancellationToken cancellationToken = default);

    /// <summary>The completed purchases of a game, to know what it still holds after a refund.</summary>
    Task<List<Purchase>> GetCompletedByGameAsync(Guid gameId, CancellationToken cancellationToken = default);

    /// <summary>The most recent purchases, newest first, for the administration.</summary>
    Task<List<Purchase>> ListRecentAsync(int count, CancellationToken cancellationToken = default);
}

public interface ISubscriptionRepository
{
    void Add(Subscription subscription);

    Task<Subscription?> GetByProviderIdAsync(string providerSubscriptionId, CancellationToken cancellationToken = default);

    /// <summary>The most recent subscription of the organizer (tracked, so it can be changed).</summary>
    Task<Subscription?> GetLatestByOrganizerAsync(string organizerId, CancellationToken cancellationToken = default);

    /// <summary>Whether the organizer has a subscription that gives the Community plan at this moment.</summary>
    Task<bool> HasActivePlanAsync(string organizerId, DateTimeOffset now, CancellationToken cancellationToken = default);

    Task<List<Subscription>> ListRecentAsync(int count, CancellationToken cancellationToken = default);
}

public interface IPaymentEventRepository
{
    Task<bool> ExistsAsync(string eventId, CancellationToken cancellationToken = default);

    void Add(PaymentEvent paymentEvent);
}
