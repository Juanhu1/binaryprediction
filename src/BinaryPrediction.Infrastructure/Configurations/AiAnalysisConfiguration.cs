using BinaryPrediction.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BinaryPrediction.Infrastructure.Configurations;

public class AiAnalysisConfiguration : IEntityTypeConfiguration<AiAnalysis>
{
    public void Configure(EntityTypeBuilder<AiAnalysis> builder)
    {
        builder.ToTable("ai_analyses");

        builder.HasKey(analysis => analysis.Id);

        builder.Property(analysis => analysis.EstimatedProbability)
            .HasPrecision(9, 6)
            .IsRequired();

        builder.Property(analysis => analysis.Confidence)
            .HasPrecision(9, 6)
            .IsRequired();

        builder.Property(analysis => analysis.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(analysis => new { analysis.MarketId, analysis.CreatedAtUtc });

        builder.HasOne<Market>()
            .WithMany()
            .HasForeignKey(analysis => analysis.MarketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
