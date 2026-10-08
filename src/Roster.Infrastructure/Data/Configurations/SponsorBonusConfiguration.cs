using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Roster.Domain.Scores;
using Roster.Domain.Games;

namespace Roster.Infrastructure.Data.Configurations;

public sealed class SponsorBonusConfiguration : IEntityTypeConfiguration<SponsorBonus>
{
    public void Configure(EntityTypeBuilder<SponsorBonus> builder)
    {
        builder.ToTable("SponsorBonuses");
        builder.HasKey(x => x.Id);

        builder.HasOne<Game>().WithMany().HasForeignKey(x => x.GameId).OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.SponsorName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.QrToken).HasMaxLength(200).IsRequired();
    }
}
