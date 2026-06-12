using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Entities.Common;
using Microsoft.EntityFrameworkCore;

namespace BinaryPrediction.Infrastructure.Persistence;

public class BinaryPredictionDbContext : DbContext
{
    public BinaryPredictionDbContext(DbContextOptions<BinaryPredictionDbContext> options)
        : base(options)
    {
    }

    public DbSet<Market> Markets { get; set; }
    public DbSet<MarketSnapshot> MarketSnapshots { get; set; }
    public DbSet<EligibleMarketView> EligibleMarketsView { get; set; }
    public DbSet<MarketAnalysisQueueItem> MarketAnalysisQueueItems { get; set; }

    public DbSet<AiAnalysis> AiAnalyses => Set<AiAnalysis>();
    public DbSet<PredictionBenchmarkResult> PredictionBenchmarkResults => Set<PredictionBenchmarkResult>();
    

    public DbSet<AiUsageRecord> AiUsageRecords { get; set; } = null!;
    public DbSet<PromptVersion> PromptVersions { get; set; } = null!;
    public DbSet<PredictionResolutionHistory> PredictionResolutionHistories { get; set; } = null!;
    public DbSet<WorkerHeartbeat> WorkerHeartbeats { get; set; } = null!;
    public DbSet<SystemHealthSnapshot> SystemHealthSnapshots => Set<SystemHealthSnapshot>();

    public DbSet<Prediction> Predictions => Set<Prediction>();
    public DbSet<PredictionQualitySnapshot> PredictionQualitySnapshots => Set<PredictionQualitySnapshot>();
    
    public DbSet<Alert> Alerts => Set<Alert>();

    // Analytics entities
    public DbSet<PredictionCategory> PredictionCategories { get; set; } = null!;
    public DbSet<PredictionCalibrationSnapshot> PredictionCalibrationSnapshots { get; set; } = null!;
    public DbSet<PredictionPerformanceSnapshot> PredictionPerformanceSnapshots => Set<PredictionPerformanceSnapshot>();
    public DbSet<CategoryPerformanceSnapshot> CategoryPerformanceSnapshots => Set<CategoryPerformanceSnapshot>();
    public DbSet<CalibrationTrendSnapshot> CalibrationTrendSnapshots => Set<CalibrationTrendSnapshot>();
    public DbSet<PredictionOpportunity> PredictionOpportunities => Set<PredictionOpportunity>();
    public DbSet<OpportunityAnalyticsSnapshot> OpportunityAnalyticsSnapshots => Set<OpportunityAnalyticsSnapshot>();
        public DbSet<OpportunityLifecycleSnapshot> OpportunityLifecycleSnapshots => Set<OpportunityLifecycleSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BinaryPredictionDbContext).Assembly);

        modelBuilder.Entity<EligibleMarketView>()
            .HasNoKey()
            .ToView("eligible_markets_view");

        // Indexes for analytics
        modelBuilder.Entity<Prediction>()
            .HasIndex(p => p.EvaluatedAtUtc);
        modelBuilder.Entity<PredictionPerformanceSnapshot>()
            .HasIndex(s => s.SnapshotDateUtc);
        // CreatedAtUtc is part of BaseEntity; apply to each entity needing it
        modelBuilder.Entity<Prediction>().HasIndex(p => p.CreatedAtUtc);
        modelBuilder.Entity<PredictionPerformanceSnapshot>().HasIndex(s => s.CreatedAtUtc);

        // PredictionOpportunity indexes
        modelBuilder.Entity<PredictionOpportunity>(entity =>
        {
            entity.HasIndex(e => e.MarketId);
            entity.HasIndex(e => e.PredictionId).IsUnique(); // Prevent duplicates per prediction
            entity.HasIndex(e => e.HasEdge);
            entity.HasIndex(e => e.ProbabilityGap);
            entity.HasIndex(e => e.DetectedAtUtc);
        });

        // OpportunityAnalyticsSnapshot indexes
        modelBuilder.Entity<OpportunityAnalyticsSnapshot>(entity =>
        {
            entity.HasIndex(e => e.SnapshotDateUtc).IsUnique();
            // Indexes for prediction resolution and evaluation timestamps
            modelBuilder.Entity<Prediction>()
                .HasIndex(p => p.ResolvedAtUtc);
            modelBuilder.Entity<Prediction>()
                .HasIndex(p => p.EvaluatedAtUtc);
        });
    }

    public override int SaveChanges()
    {
        ApplyTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        ApplyTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyTimestamps()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = now;
                    break;
                case EntityState.Modified:
                    entry.Property(entity => entity.CreatedAtUtc).IsModified = false;
                    if (entry.Entity is AuditableEntity auditableEntity)
                    {
                        auditableEntity.UpdatedAtUtc = now;
                    }
                    break;
            }
        }
    }
}
