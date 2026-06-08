using BinaryPrediction.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BinaryPrediction.Infrastructure.Persistence.Configurations;

public class PredictionQualitySnapshotConfiguration : IEntityTypeConfiguration<PredictionQualitySnapshot>
{
    public void Configure(EntityTypeBuilder<PredictionQualitySnapshot> builder)
    {
        builder.ToTable("prediction_quality_snapshots");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.SnapshotDateUtc)
            .HasColumnName("snapshot_date_utc")
            .IsRequired();

        builder.Property(x => x.TotalPredictions)
            .HasColumnName("total_predictions")
            .IsRequired();

        builder.Property(x => x.AccuracyPercentage)
            .HasColumnName("accuracy_percentage")
            .IsRequired();

        builder.Property(x => x.AverageBrierScore)
            .HasColumnName("average_brier_score")
            .IsRequired();

        builder.Property(x => x.CalibrationError)
            .HasColumnName("calibration_error")
            .IsRequired();

        builder.Property(x => x.BenchmarkAdvantage)
            .HasColumnName("benchmark_advantage")
            .IsRequired();

        builder.Property(x => x.ImprovementTrend)
            .HasColumnName("improvement_trend")
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasIndex(x => x.SnapshotDateUtc);
        builder.HasIndex(x => x.CreatedAtUtc);
    }
}
