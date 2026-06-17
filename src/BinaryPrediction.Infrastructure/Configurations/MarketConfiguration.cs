using BinaryPrediction.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BinaryPrediction.Infrastructure.Configurations;

public class MarketConfiguration : IEntityTypeConfiguration<Market>
{
    public void Configure(EntityTypeBuilder<Market> builder)
    {
        builder.ToTable("markets");

        builder.HasKey(market => market.Id);

        builder.Property(market => market.Question)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(market => market.Slug)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(market => market.Liquidity)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(market => market.Volume)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(market => market.CreatedAtUtc)
            .IsRequired();

        builder.Property(market => market.ActualOutcome)
            .HasMaxLength(255);

        builder.Property(market => market.ExternalMarketId)
            .HasMaxLength(200);

        builder.Property(market => market.ExternalEventId)
            .HasMaxLength(200);

        builder.Property(market => market.SourceUrl)
            .HasMaxLength(500);

        builder.HasIndex(market => new { market.MarketSource, market.Slug })
            .IsUnique();

        builder.HasIndex(market => new { market.MarketSource, market.ExternalMarketId })
            .IsUnique();

        builder.HasIndex(market => new { market.Active, market.Closed });
        builder.HasIndex(market => market.ResolvedAtUtc);
        builder.HasOne(market => market.PredictionCategory)
            .WithMany()
            .HasForeignKey(market => market.PredictionCategoryId)
            .IsRequired(false);
    }
}
