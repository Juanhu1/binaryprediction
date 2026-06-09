using BinaryPrediction.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BinaryPrediction.Infrastructure.Persistence.Configurations;

public class WorkerHeartbeatConfiguration : IEntityTypeConfiguration<WorkerHeartbeat>
{
    public void Configure(EntityTypeBuilder<WorkerHeartbeat> builder)
    {
        builder.ToTable("worker_heartbeats");

        builder.HasKey(x => x.WorkerName);

        builder.Property(x => x.WorkerName)
            .HasColumnName("worker_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.LastHeartbeatUtc)
            .HasColumnName("last_heartbeat_utc")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.LastErrorMessage)
            .HasColumnName("last_error_message");
    }
}
