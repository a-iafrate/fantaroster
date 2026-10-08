using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Roster.Domain.Participants;
using Roster.Domain.Games;

namespace Roster.Infrastructure.Data.Configurations;

public sealed class ParticipantConfiguration : IEntityTypeConfiguration<Participant>
{
    public void Configure(EntityTypeBuilder<Participant> builder)
    {
        builder.ToTable("Participants");
        builder.HasKey(x => x.Id);

        builder.HasOne<Game>().WithMany().HasForeignKey(x => x.GameId).OnDelete(DeleteBehavior.Cascade);

        // Unique nickname per game
        builder.HasIndex(x => new { x.GameId, x.Nickname }).IsUnique();

        builder.Property(x => x.Nickname).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TokenHash).HasMaxLength(200).IsRequired();
    }
}
