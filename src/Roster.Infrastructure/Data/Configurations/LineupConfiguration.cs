using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Roster.Domain.Participants;

namespace Roster.Infrastructure.Data.Configurations;

public sealed class LineupConfiguration : IEntityTypeConfiguration<Lineup>
{
    public void Configure(EntityTypeBuilder<Lineup> builder)
    {
        builder.ToTable("Lineups");
        builder.HasKey(x => x.ParticipantId);

        builder.HasOne<Participant>().WithOne().HasForeignKey<Lineup>(x => x.ParticipantId).OnDelete(DeleteBehavior.Cascade);

        // Store PickedElementIds as JSON string or varbinary, EF Core 8+ supports primitive collections
        // But rule says EF Core 10, so arrays map naturally to JSON in SQL Server.
        // Actually, just let EF Core handle Guid[] natively or configure it as primitive collection.
        builder.Property(x => x.PickedElementIds).HasColumnType("nvarchar(max)");
    }
}
