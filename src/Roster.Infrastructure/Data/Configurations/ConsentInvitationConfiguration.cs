using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Roster.Domain.Elements;

namespace Roster.Infrastructure.Data.Configurations;

public sealed class ConsentInvitationConfiguration : IEntityTypeConfiguration<ConsentInvitation>
{
    public void Configure(EntityTypeBuilder<ConsentInvitation> builder)
    {
        builder.ToTable("ConsentInvitations");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.TokenHash).IsRequired().HasMaxLength(256);
        builder.Property(c => c.Contact).HasMaxLength(256);

        builder.HasOne<Element>()
            .WithMany()
            .HasForeignKey(c => c.ElementId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
