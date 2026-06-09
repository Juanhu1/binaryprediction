using BinaryPrediction.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BinaryPrediction.Infrastructure.Persistence.Configurations;

public class SystemHealthSnapshotConfiguration : IEntityTypeConfiguration<SystemHealthSnapshot>
{
    public void Configure(EntityTypeBuilder<SystemHealthSnapshot> builder)
    {
        builder.ToTable("system_health_snapshots");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(s => s.CreatedAtUtc);
    }
}
