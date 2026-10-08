using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Roster.Domain.Scores;
using Roster.Domain.Games;
using Roster.Domain.Elements;
using Roster.Domain.Participants;

namespace Roster.Infrastructure.Data.Configurations;

public sealed class ScoreEntryConfiguration : IEntityTypeConfiguration<ScoreEntry>
{
    public void Configure(EntityTypeBuilder<ScoreEntry> builder)
    {
        builder.ToTable("ScoreEntries");
        builder.HasKey(x => x.Id);

        builder.HasOne<Game>().WithMany().HasForeignKey(x => x.GameId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Element>().WithMany().HasForeignKey(x => x.ElementId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Participant>().WithMany().HasForeignKey(x => x.ParticipantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Rule>().WithMany().HasForeignKey(x => x.RuleId).OnDelete(DeleteBehavior.Restrict);

        // Unique idempotency key per game
        builder.HasIndex(x => new { x.GameId, x.IdempotencyKey }).IsUnique();

        builder.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.VoidedBy).HasMaxLength(200);
        builder.Property(x => x.Note).HasMaxLength(1000);
    }
}
