using BinaryPrediction.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BinaryPrediction.Infrastructure.Configurations;

public class CategoryPerformanceSnapshotConfiguration : IEntityTypeConfiguration<CategoryPerformanceSnapshot>
{
    public void Configure(EntityTypeBuilder<CategoryPerformanceSnapshot> builder)
    {
        builder.ToTable("category_performance_snapshots");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.SnapshotDateUtc)
            .HasConversion(
                v => DateTime.SpecifyKind(v.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc),
                v => DateOnly.FromDateTime(v))
            .IsRequired();
        builder.HasIndex(c => new { c.PredictionCategoryId, c.SnapshotDateUtc }).IsUnique();
        builder.Property(c => c.CreatedAtUtc).IsRequired();
    }
}
