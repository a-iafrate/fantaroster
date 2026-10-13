using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Roster.Domain.Payments;

namespace Roster.Infrastructure.Data.Configurations;

public sealed class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("Purchases");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.OrganizerId).IsRequired().HasMaxLength(450);
        builder.Property(p => p.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Amount).HasColumnType("decimal(18,2)");
        builder.Property(p => p.Currency).IsRequired().HasMaxLength(3);
        builder.Property(p => p.ProviderOrderId).IsRequired().HasMaxLength(100);
        builder.Property(p => p.ProviderCaptureId).HasMaxLength(100);

        builder.HasIndex(p => p.ProviderOrderId).IsUnique();
        builder.HasIndex(p => p.ProviderCaptureId);
        builder.HasIndex(p => p.OrganizerId);
        builder.HasIndex(p => p.GameId);
    }
}

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.OrganizerId).IsRequired().HasMaxLength(450);
        builder.Property(s => s.ProviderSubscriptionId).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(s => s.ProviderSubscriptionId).IsUnique();
        builder.HasIndex(s => s.OrganizerId);
    }
}

public sealed class PaymentEventConfiguration : IEntityTypeConfiguration<PaymentEvent>
{
    public void Configure(EntityTypeBuilder<PaymentEvent> builder)
    {
        builder.ToTable("PaymentEvents");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasMaxLength(100);
        builder.Property(e => e.Type).IsRequired().HasMaxLength(100);
    }
}
