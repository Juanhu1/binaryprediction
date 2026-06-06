using BinaryPrediction.Core.Common;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Enums;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BinaryPrediction.Infrastructure.Services;

public class MarketAnalysisQueueService : IMarketAnalysisQueueService
{
    private readonly BinaryPredictionDbContext _dbContext;
    private readonly ILogger<MarketAnalysisQueueService> _logger;
    private readonly QueueProcessingSettings _settings;
    private readonly AnalysisRefreshSettings _refreshSettings;

    public MarketAnalysisQueueService(
        BinaryPredictionDbContext dbContext,
        ILogger<MarketAnalysisQueueService> logger,
        IOptions<QueueProcessingSettings> options,
        IOptions<AnalysisRefreshSettings> refreshSettings)
    {
        _dbContext = dbContext;
        _logger = logger;
        _settings = options.Value;
        _refreshSettings = refreshSettings.Value;
    }

    public async Task EnqueueEligibleMarketsAsync(CancellationToken cancellationToken = default)
    {
        var eligibleMarkets = await _dbContext.EligibleMarketsView
            .Select(m => new { m.Id, m.Category })
            .ToListAsync(cancellationToken);

        var queuedMarketIds = await _dbContext.MarketAnalysisQueueItems
            .Where(q => q.Status == AnalysisQueueStatus.Pending 
                     || q.Status == AnalysisQueueStatus.Processing)
            .Select(q => q.MarketId)
            .ToListAsync(cancellationToken);

        var eligibleMarketIds = eligibleMarkets.Select(m => m.Id).ToList();

        var latestAnalyses = await _dbContext.Set<AiAnalysis>()
            .Where(a => eligibleMarketIds.Contains(a.MarketId))
            .GroupBy(a => a.MarketId)
            .Select(g => new { MarketId = g.Key, LastAnalysisUtc = g.Max(a => a.CreatedAtUtc) })
            .ToDictionaryAsync(x => x.MarketId, x => x.LastAnalysisUtc, cancellationToken);

        var marketsToEnqueue = new List<Guid>();

        foreach (var market in eligibleMarkets)
        {
            if (queuedMarketIds.Contains(market.Id)) continue;

            if (latestAnalyses.TryGetValue(market.Id, out var lastAnalysisUtc))
            {
                var refreshThreshold = _refreshSettings.GetRefreshHoursForCategory(market.Category);
                var ageHours = (DateTimeOffset.UtcNow - lastAnalysisUtc).TotalHours;
                if (ageHours < refreshThreshold)
                {
                    _logger.LogInformation("Skipping analysis for market {MarketId}. Last analysis age: {Hours:F1} hours.", market.Id, ageHours);
                    continue;
                }
            }

            marketsToEnqueue.Add(market.Id);
        }

        _logger.LogInformation(
            "QueueWorker: Eligible={EligibleCount}, ExistingActive={ExistingCount}, ToQueue={ToQueueCount}",
            eligibleMarkets.Count,
            queuedMarketIds.Count,
            marketsToEnqueue.Count);
            
        if (eligibleMarkets.Any())
        {
            _logger.LogInformation("First few eligible IDs: {Ids}", string.Join(", ", eligibleMarkets.Take(3).Select(e => e.Id)));
        }
        
        if (queuedMarketIds.Any())
        {
            _logger.LogInformation("First few existing active IDs: {Ids}", string.Join(", ", queuedMarketIds.Take(3)));
        }

        if (!marketsToEnqueue.Any()) return;

        var items = marketsToEnqueue.Select(marketId => new MarketAnalysisQueueItem
        {
            MarketId = marketId,
            Status = AnalysisQueueStatus.Pending
        }).ToList();

        _logger.LogInformation("Creating {Count} queue entries", items.Count);

        await _dbContext.MarketAnalysisQueueItems.AddRangeAsync(items, cancellationToken);
        
        _logger.LogInformation("Before SaveChangesAsync()");
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("After SaveChangesAsync()");
        
        _logger.LogInformation("Queue entries saved successfully");

        foreach (var marketId in marketsToEnqueue)
        {
            _logger.LogInformation("Enqueued market for analysis: {MarketId}", marketId);
        }
    }

    public async Task<IReadOnlyList<MarketAnalysisQueueItem>> GetPendingItemsAsync(int limit, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MarketAnalysisQueueItems
            .Where(q => q.Status == AnalysisQueueStatus.Pending)
            .OrderByDescending(q => q.Priority)
            .ThenBy(q => q.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkProcessingAsync(Guid queueItemId, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.MarketAnalysisQueueItems.FindAsync([queueItemId], cancellationToken);
        if (item != null && item.Status == AnalysisQueueStatus.Pending)
        {
            item.Status = AnalysisQueueStatus.Processing;
            item.StartedAtUtc = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Queue item started: {QueueItemId}", queueItemId);
        }
    }

    public async Task MarkCompletedAsync(Guid queueItemId, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.MarketAnalysisQueueItems.FindAsync([queueItemId], cancellationToken);
        if (item != null && item.Status == AnalysisQueueStatus.Processing)
        {
            var analysis = await _dbContext.Set<AiAnalysis>().FirstOrDefaultAsync(a => a.MarketId == item.MarketId, cancellationToken);
            if (analysis == null || analysis.Id == Guid.Empty)
            {
                throw new InvalidOperationException($"Cannot complete queue item {queueItemId}: No valid AI analysis record found.");
            }

            item.Status = AnalysisQueueStatus.Completed;
            item.CompletedAtUtc = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("AI analysis saved: {AnalysisId}", analysis.Id);
            _logger.LogInformation("Queue item completed: {QueueItemId}", queueItemId);
        }
    }

    public async Task MarkFailedAsync(Guid queueItemId, string errorMessage, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.MarketAnalysisQueueItems.FindAsync([queueItemId], cancellationToken);
        if (item != null && item.Status == AnalysisQueueStatus.Processing)
        {
            item.LastError = errorMessage;
            item.RetryCount++;

            if (item.RetryCount >= _settings.MaxRetries)
            {
                item.Status = AnalysisQueueStatus.Failed;
                _logger.LogWarning("Queue item failed: {QueueItemId}", queueItemId);
                _logger.LogWarning("Failure reason: {Error}", errorMessage);
            }
            else
            {
                item.Status = AnalysisQueueStatus.Pending; // requeue for retry
                _logger.LogWarning("Queue item retried: {QueueItemId}. RetryCount: {RetryCount}", queueItemId, item.RetryCount);
            }

            item.CompletedAtUtc = null;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RecoverStuckItemsAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-30);
        var stuckItems = await _dbContext.MarketAnalysisQueueItems
            .Where(q => q.Status == AnalysisQueueStatus.Processing && q.StartedAtUtc < cutoff)
            .ToListAsync(cancellationToken);

        foreach (var item in stuckItems)
        {
            item.Status = AnalysisQueueStatus.Pending;
            item.RetryCount++;
            _logger.LogInformation("Recovered stuck queue item {Id}", item.Id);
        }

        if (stuckItems.Any())
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task LogQueueStatusAsync(CancellationToken cancellationToken = default)
    {
        var statuses = await _dbContext.MarketAnalysisQueueItems
            .GroupBy(q => q.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        int pending = statuses.FirstOrDefault(s => s.Status == AnalysisQueueStatus.Pending)?.Count ?? 0;
        int processing = statuses.FirstOrDefault(s => s.Status == AnalysisQueueStatus.Processing)?.Count ?? 0;
        int completed = statuses.FirstOrDefault(s => s.Status == AnalysisQueueStatus.Completed)?.Count ?? 0;
        int failed = statuses.FirstOrDefault(s => s.Status == AnalysisQueueStatus.Failed)?.Count ?? 0;

        var todayUtc = new DateTimeOffset(DateTimeOffset.UtcNow.UtcDateTime.Date, TimeSpan.Zero);
        var analysesToday = await _dbContext.Set<AiAnalysis>()
            .CountAsync(a => a.CreatedAtUtc >= todayUtc, cancellationToken);
            
        // Note: Failed calls roughly correspond to OpenAI attempts that didn't complete
        var failedToday = await _dbContext.MarketAnalysisQueueItems
            .CountAsync(q => q.Status == AnalysisQueueStatus.Failed && q.CompletedAtUtc >= todayUtc, cancellationToken);
        var totalCallsApproximation = analysesToday + failedToday;

        _logger.LogInformation(
            "Queue status:\nPending={Pending}\nProcessing={Processing}\nCompleted={Completed}\nFailed={Failed}\nAI analyses created today: {AnalysesToday}\nOpenAI calls today: {CallsToday}",
            pending, processing, completed, failed, analysesToday, totalCallsApproximation);
    }
}
