using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Roster.Domain.Organizers;

namespace Roster.Infrastructure.Data.Configurations;

public sealed class CommunityRequestConfiguration : IEntityTypeConfiguration<CommunityRequest>
{
    public void Configure(EntityTypeBuilder<CommunityRequest> builder)
    {
        builder.ToTable("CommunityRequests");
        builder.HasKey(r => r.Id);

        // Same length as the identity user id it points to; no foreign key, so a request outlives nothing it should not.
        builder.Property(r => r.OrganizerId).IsRequired().HasMaxLength(450);
        builder.Property(r => r.OrganizerEmail).IsRequired().HasMaxLength(256);
        builder.Property(r => r.Description).IsRequired().HasMaxLength(2000);
        builder.Property(r => r.Link).HasMaxLength(500);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.DecidedBy).HasMaxLength(256);
        builder.Property(r => r.DecisionNote).HasMaxLength(1000);

        builder.HasIndex(r => r.OrganizerId);
    }
}
