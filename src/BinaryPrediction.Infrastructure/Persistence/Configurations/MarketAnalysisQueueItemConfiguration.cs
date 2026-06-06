using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BinaryPrediction.Infrastructure.Persistence.Configurations;

public class MarketAnalysisQueueItemConfiguration : IEntityTypeConfiguration<MarketAnalysisQueueItem>
{
    public void Configure(EntityTypeBuilder<MarketAnalysisQueueItem> builder)
    {
        builder.ToTable("market_analysis_queue");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.Status)
            .HasConversion<string>()
            .IsRequired();

        // One-to-many relationship with Market
        builder.HasOne(q => q.Market)
            .WithMany()
            .HasForeignKey(q => q.MarketId)
            .OnDelete(DeleteBehavior.Cascade);

        // Required indexes for querying queue
        builder.HasIndex(q => q.Status);
        builder.HasIndex(q => q.Priority);
        builder.HasIndex(q => q.CreatedAtUtc);
        builder.HasIndex(q => q.MarketId);

        // Unique partial index to prevent duplicates
        builder.HasIndex(q => q.MarketId)
            .IsUnique()
            .HasFilter("status IN ('Pending', 'Processing')")
            .HasDatabaseName("ux_market_analysis_queue_active");
    }
}
