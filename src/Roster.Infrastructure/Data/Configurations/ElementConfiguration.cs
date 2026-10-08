using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Roster.Domain.Elements;
using Roster.Domain.Games;
using Roster.Domain.Sources;

namespace Roster.Infrastructure.Data.Configurations;

public sealed class ElementConfiguration : IEntityTypeConfiguration<Element>
{
    public void Configure(EntityTypeBuilder<Element> builder)
    {
        builder.ToTable("Elements");
        builder.HasKey(x => x.Id);

        builder.HasOne<Game>().WithMany().HasForeignKey(x => x.GameId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<SourceBinding>().WithMany().HasForeignKey(x => x.SourceBindingId).OnDelete(DeleteBehavior.Restrict);

        // External ID per source binding uniqueness
        builder.HasIndex(x => new { x.SourceBindingId, x.ExternalId }).IsUnique().HasFilter("[SourceBindingId] IS NOT NULL AND [ExternalId] IS NOT NULL");

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Subtitle).HasMaxLength(500);
        builder.Property(x => x.ImageUrl).HasMaxLength(2000);
        builder.Property(x => x.Group).HasMaxLength(200);
        builder.Property(x => x.ExternalId).HasMaxLength(200);

        builder.Property(x => x.MetadataJson).IsRequired();
    }
}
