using BinaryPrediction.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BinaryPrediction.Infrastructure.Persistence.Configurations;

public class PromptVersionConfiguration : IEntityTypeConfiguration<PromptVersion>
{
    public void Configure(EntityTypeBuilder<PromptVersion> builder)
    {
        builder.ToTable("prompt_versions");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.Version).HasColumnName("version").IsRequired().HasMaxLength(50);
        builder.Property(x => x.PromptName).HasColumnName("prompt_name").IsRequired().HasMaxLength(100);
        builder.Property(x => x.PromptTemplate).HasColumnName("prompt_template").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        
        builder.HasIndex(x => x.Version).IsUnique();
        builder.HasIndex(x => x.PromptName);
    }
}
