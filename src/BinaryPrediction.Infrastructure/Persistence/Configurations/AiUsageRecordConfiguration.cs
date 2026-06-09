using BinaryPrediction.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BinaryPrediction.Infrastructure.Persistence.Configurations;

public class AiUsageRecordConfiguration : IEntityTypeConfiguration<AiUsageRecord>
{
    public void Configure(EntityTypeBuilder<AiUsageRecord> builder)
    {
        builder.ToTable("ai_usage_records");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.MarketId).HasColumnName("market_id");
        builder.Property(x => x.OperationType).HasColumnName("operation_type").IsRequired().HasMaxLength(50);
        builder.Property(x => x.Model).HasColumnName("model").IsRequired().HasMaxLength(100);
        builder.Property(x => x.PromptTokens).HasColumnName("prompt_tokens");
        builder.Property(x => x.CompletionTokens).HasColumnName("completion_tokens");
        builder.Property(x => x.TotalTokens).HasColumnName("total_tokens");
        builder.Property(x => x.EstimatedCostUsd).HasColumnName("estimated_cost_usd");
        builder.Property(x => x.IsSuccess).HasColumnName("is_success");
        builder.Property(x => x.LatencyMs).HasColumnName("latency_ms");
        builder.Property(x => x.ErrorMessage).HasColumnName("error_message");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasIndex(x => x.OperationType);
        builder.HasIndex(x => x.Model);
    }
}
