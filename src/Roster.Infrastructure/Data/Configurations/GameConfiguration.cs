using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Roster.Domain.Games;

namespace Roster.Infrastructure.Data.Configurations;

public sealed class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.ToTable("Games");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Slug).IsUnique();

        builder.Property(x => x.Slug).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DomainPackId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Brand).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Culture).HasMaxLength(20).IsRequired();
        builder.Property(x => x.JoinCode).HasMaxLength(50).IsRequired();

        // Same length as the identity user id it points to; no foreign key, so a game outlives a deleted account until cleaned up.
        builder.Property(x => x.OrganizerId).HasMaxLength(450);
        builder.HasIndex(x => x.OrganizerId);

        builder.Property(x => x.LimitLabel).HasMaxLength(100);
        builder.Property(x => x.PurchasedPlan).HasConversion<string>().HasMaxLength(20);

        builder.Property(x => x.CaptainMultiplier).HasPrecision(18, 4);

        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
