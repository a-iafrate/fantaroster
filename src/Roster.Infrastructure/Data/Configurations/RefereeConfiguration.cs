using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Roster.Domain.Referees;
using Roster.Domain.Games;

namespace Roster.Infrastructure.Data.Configurations;

public sealed class RefereeConfiguration : IEntityTypeConfiguration<Referee>
{
    public void Configure(EntityTypeBuilder<Referee> builder)
    {
        builder.ToTable("Referees");
        builder.HasKey(x => x.Id);

        builder.HasOne<Game>().WithMany().HasForeignKey(x => x.GameId).OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.InviteTokenHash).HasMaxLength(200).IsRequired();
        // EF Core 8+ supports primitive collections like string[] to JSON mapping automatically
        builder.Property(x => x.Scope).HasColumnType("nvarchar(max)");
    }
}
