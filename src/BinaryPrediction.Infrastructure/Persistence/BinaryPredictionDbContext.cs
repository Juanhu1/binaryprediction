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
    
    public DbSet<Prediction> Predictions => Set<Prediction>();
    public DbSet<PredictionQualitySnapshot> PredictionQualitySnapshots => Set<PredictionQualitySnapshot>();

    public DbSet<Alert> Alerts => Set<Alert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BinaryPredictionDbContext).Assembly);

        modelBuilder.Entity<EligibleMarketView>()
            .HasNoKey()
            .ToView("eligible_markets_view");
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
