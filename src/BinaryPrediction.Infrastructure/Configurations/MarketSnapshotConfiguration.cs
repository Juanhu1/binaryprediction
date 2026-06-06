using BinaryPrediction.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BinaryPrediction.Infrastructure.Configurations;

public class MarketSnapshotConfiguration : IEntityTypeConfiguration<MarketSnapshot>
{
    public void Configure(EntityTypeBuilder<MarketSnapshot> builder)
    {
        builder.ToTable("market_snapshots");

        builder.HasKey(snapshot => snapshot.Id);

        builder.Property(snapshot => snapshot.Probability)
            .HasPrecision(9, 6)
            .IsRequired();

        builder.Property(snapshot => snapshot.Liquidity)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(snapshot => snapshot.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(snapshot => new { snapshot.MarketId, snapshot.CreatedAtUtc });

        builder.HasOne<Market>()
            .WithMany()
            .HasForeignKey(snapshot => snapshot.MarketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
