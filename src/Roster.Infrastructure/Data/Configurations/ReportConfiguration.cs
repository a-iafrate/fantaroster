using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Roster.Domain.Scores;
using Roster.Domain.Games;
using Roster.Domain.Elements;
using Roster.Domain.Participants;

namespace Roster.Infrastructure.Data.Configurations;

public sealed class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("Reports");
        builder.HasKey(x => x.Id);

        builder.HasOne<Game>().WithMany().HasForeignKey(x => x.GameId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Element>().WithMany().HasForeignKey(x => x.ElementId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Participant>().WithMany().HasForeignKey(x => x.ParticipantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Rule>().WithMany().HasForeignKey(x => x.SuggestedRuleId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Text).HasMaxLength(2000);
        builder.Property(x => x.PhotoBlobName).HasMaxLength(500);
    }
}
