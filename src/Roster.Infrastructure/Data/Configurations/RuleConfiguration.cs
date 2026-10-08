using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Roster.Domain.Games;

namespace Roster.Infrastructure.Data.Configurations;

public sealed class RuleConfiguration : IEntityTypeConfiguration<Rule>
{
    public void Configure(EntityTypeBuilder<Rule> builder)
    {
        builder.ToTable("Rules");
        builder.HasKey(x => x.Id);

        builder.HasOne<Game>().WithMany().HasForeignKey(x => x.GameId).OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.Label).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(100).IsRequired();
    }
}
