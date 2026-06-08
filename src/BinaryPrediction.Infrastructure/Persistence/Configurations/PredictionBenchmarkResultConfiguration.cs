using BinaryPrediction.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BinaryPrediction.Infrastructure.Persistence.Configurations;

public class PredictionBenchmarkResultConfiguration : IEntityTypeConfiguration<PredictionBenchmarkResult>
{
    public void Configure(EntityTypeBuilder<PredictionBenchmarkResult> builder)
    {
        builder.ToTable("prediction_benchmark_results");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(p => p.BenchmarkType)
            .HasColumnName("benchmark_type")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.TotalPredictions)
            .HasColumnName("total_predictions")
            .IsRequired();

        builder.Property(p => p.CorrectPredictions)
            .HasColumnName("correct_predictions")
            .IsRequired();

        builder.Property(p => p.AccuracyPercentage)
            .HasColumnName("accuracy_percentage")
            .IsRequired()
            .HasColumnType("numeric(5,2)");

        builder.Property(p => p.AverageBrierScore)
            .HasColumnName("average_brier_score")
            .IsRequired()
            .HasColumnType("numeric(6,4)");

        builder.Property(p => p.CalculatedAtUtc)
            .HasColumnName("calculated_at_utc")
            .IsRequired();

        builder.Property(p => p.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasIndex(p => p.BenchmarkType)
            .HasDatabaseName("ix_prediction_benchmark_results_benchmark_type");

        builder.HasIndex(p => p.CalculatedAtUtc)
            .HasDatabaseName("ix_prediction_benchmark_results_calculated_at_utc");
    }
}
