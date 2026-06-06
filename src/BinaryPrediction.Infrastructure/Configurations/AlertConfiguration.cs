using BinaryPrediction.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BinaryPrediction.Infrastructure.Configurations;

public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("alerts");

        builder.HasKey(alert => alert.Id);

        builder.Property(alert => alert.Message)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(alert => alert.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(alert => new { alert.MarketId, alert.CreatedAtUtc });

        builder.HasOne<Market>()
            .WithMany()
            .HasForeignKey(alert => alert.MarketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
