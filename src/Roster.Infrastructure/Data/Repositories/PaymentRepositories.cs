using Microsoft.EntityFrameworkCore;
using Roster.Application.Ports.Data;
using Roster.Domain.Payments;

namespace Roster.Infrastructure.Data.Repositories;

internal sealed class PurchaseRepository(RosterDbContext context) : IPurchaseRepository
{
    public void Add(Purchase purchase) => context.Purchases.Add(purchase);

    public Task<Purchase?> GetByOrderIdAsync(string providerOrderId, CancellationToken cancellationToken = default) =>
        context.Purchases.FirstOrDefaultAsync(p => p.ProviderOrderId == providerOrderId, cancellationToken);

    public Task<Purchase?> GetByCaptureIdAsync(string providerCaptureId, CancellationToken cancellationToken = default) =>
        context.Purchases.FirstOrDefaultAsync(p => p.ProviderCaptureId == providerCaptureId, cancellationToken);

    public Task<List<Purchase>> GetCompletedByGameAsync(Guid gameId, CancellationToken cancellationToken = default) =>
        context.Purchases.Where(p => p.GameId == gameId && p.Status == PurchaseStatus.Completed).ToListAsync(cancellationToken);

    public Task<List<Purchase>> ListRecentAsync(int count, CancellationToken cancellationToken = default) =>
        context.Purchases.AsNoTracking().OrderByDescending(p => p.CreatedAt).Take(count).ToListAsync(cancellationToken);
}

internal sealed class SubscriptionRepository(RosterDbContext context) : ISubscriptionRepository
{
    public void Add(Subscription subscription) => context.Subscriptions.Add(subscription);

    public Task<Subscription?> GetByProviderIdAsync(string providerSubscriptionId, CancellationToken cancellationToken = default) =>
        context.Subscriptions.FirstOrDefaultAsync(s => s.ProviderSubscriptionId == providerSubscriptionId, cancellationToken);

    public async Task<Subscription?> GetLatestByOrganizerAsync(string organizerId, CancellationToken cancellationToken = default)
    {
        // Ordered in memory: a handful of rows per organizer, and the date type is not translated by every provider.
        var all = await context.Subscriptions.Where(s => s.OrganizerId == organizerId).ToListAsync(cancellationToken);
        return all.OrderByDescending(s => s.CreatedAt).FirstOrDefault();
    }

    public async Task<bool> HasActivePlanAsync(string organizerId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var all = await context.Subscriptions.AsNoTracking().Where(s => s.OrganizerId == organizerId).ToListAsync(cancellationToken);
        return all.Any(s => s.GrantsPlan(now));
    }

    public async Task<List<Subscription>> ListRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        var all = await context.Subscriptions.AsNoTracking().ToListAsync(cancellationToken);
        return all.OrderByDescending(s => s.CreatedAt).Take(count).ToList();
    }
}

internal sealed class PaymentEventRepository(RosterDbContext context) : IPaymentEventRepository
{
    public Task<bool> ExistsAsync(string eventId, CancellationToken cancellationToken = default) =>
        context.PaymentEvents.AnyAsync(e => e.Id == eventId, cancellationToken);

    public void Add(PaymentEvent paymentEvent) => context.PaymentEvents.Add(paymentEvent);
}
