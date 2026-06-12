using BinaryPrediction.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BinaryPrediction.Infrastructure.Configurations;

public class PredictionPerformanceSnapshotConfiguration : IEntityTypeConfiguration<PredictionPerformanceSnapshot>
{
    public void Configure(EntityTypeBuilder<PredictionPerformanceSnapshot> builder)
    {
        builder.ToTable("prediction_performance_snapshots");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.SnapshotDateUtc)
            .HasConversion(
                v => DateTime.SpecifyKind(v.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc),
                v => DateOnly.FromDateTime(v))
            .HasColumnType("date")
            .IsRequired();
        builder.HasIndex(p => p.SnapshotDateUtc).IsUnique();
        builder.Property(p => p.CreatedAtUtc).IsRequired();
    }
}
