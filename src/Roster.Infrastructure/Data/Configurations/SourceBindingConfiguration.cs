using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Roster.Domain.Sources;
using Roster.Domain.Games;

namespace Roster.Infrastructure.Data.Configurations;

public sealed class SourceBindingConfiguration : IEntityTypeConfiguration<SourceBinding>
{
    public void Configure(EntityTypeBuilder<SourceBinding> builder)
    {
        builder.ToTable("SourceBindings");
        builder.HasKey(x => x.Id);

        builder.HasOne<Game>().WithMany().HasForeignKey(x => x.GameId).OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.PluginId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ConfigurationJson).IsRequired();
        builder.Property(x => x.SecretReference).HasMaxLength(200);
        builder.Property(x => x.LastSyncResult).HasMaxLength(1000);
    }
}
