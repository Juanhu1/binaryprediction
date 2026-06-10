using BinaryPrediction.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BinaryPrediction.Infrastructure.Configurations;

public class CalibrationTrendSnapshotConfiguration : IEntityTypeConfiguration<CalibrationTrendSnapshot>
{
    public void Configure(EntityTypeBuilder<CalibrationTrendSnapshot> builder)
    {
        builder.ToTable("calibration_trend_snapshots");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.SnapshotDateUtc)
            .HasConversion(
                v => DateTime.SpecifyKind(v.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc),
                v => DateOnly.FromDateTime(v))
            .IsRequired();
        builder.HasIndex(c => new { c.ConfidenceRange, c.SnapshotDateUtc }).IsUnique();
        builder.Property(c => c.CreatedAtUtc).IsRequired();
    }
}
