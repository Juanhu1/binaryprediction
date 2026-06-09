using BinaryPrediction.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BinaryPrediction.Infrastructure.Persistence.Configurations;

public class PredictionConfiguration : IEntityTypeConfiguration<Prediction>
{
    public void Configure(EntityTypeBuilder<Prediction> builder)
    {
        builder.ToTable("predictions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(p => p.MarketId)
            .HasColumnName("market_id")
            .IsRequired();

        builder.Property(p => p.AnalysisId)
            .HasColumnName("analysis_id")
            .IsRequired();

        builder.Property(p => p.PredictedOutcome)
            .HasColumnName("predicted_outcome")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(p => p.ConfidencePercentage)
            .HasColumnName("confidence_percentage")
            .HasColumnType("numeric(5,2)")
            .IsRequired();

        builder.Property(p => p.ReasoningSummary)
            .HasColumnName("reasoning_summary")
            .IsRequired();

        builder.Property(p => p.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(p => p.ExpiresAtUtc)
            .HasColumnName("expires_at_utc");

        builder.Property(p => p.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(p => p.ActualOutcome)
            .HasColumnName("actual_outcome")
            .HasMaxLength(255);

        builder.Property(p => p.WasCorrect)
            .HasColumnName("was_correct");

        builder.Property(p => p.EvaluatedAtUtc)
            .HasColumnName("evaluated_at_utc");

        builder.Property(p => p.BrierScore)
            .HasColumnName("brier_score")
            .HasColumnType("numeric(10,4)");

        builder.HasIndex(p => p.MarketId);
        builder.HasIndex(p => p.AnalysisId).IsUnique();
        builder.HasIndex(p => p.IsActive);
        builder.HasIndex(p => p.CreatedAtUtc);
        
        // New indexes for resolution queries
        builder.HasIndex(p => p.WasCorrect);
        builder.HasIndex(p => p.EvaluatedAtUtc);
        builder.HasIndex(p => p.ConfidencePercentage);

        // Map to Market
        builder.HasOne(p => p.Market)
            .WithMany()
            .HasForeignKey(p => p.MarketId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // Map to AiAnalysis
        builder.HasOne<AiAnalysis>()
            .WithMany()
            .HasForeignKey(p => p.AnalysisId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
