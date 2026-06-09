using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Enums;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BinaryPrediction.Infrastructure.Services;

public class SystemHealthService : ISystemHealthService
{
    private readonly BinaryPredictionDbContext _dbContext;
    private readonly ILogger<SystemHealthService> _logger;
    private readonly IQueueMonitoringService _queueMonitoringService;
    private readonly IAccuracyMonitoringService _accuracyMonitoringService;

    public SystemHealthService(
        BinaryPredictionDbContext dbContext,
        ILogger<SystemHealthService> logger,
        IQueueMonitoringService queueMonitoringService,
        IAccuracyMonitoringService accuracyMonitoringService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _queueMonitoringService = queueMonitoringService;
        _accuracyMonitoringService = accuracyMonitoringService;
    }

    public async Task<SystemHealthDto> GetCurrentHealthAsync(CancellationToken cancellationToken = default)
    {
        var heartbeats = await _dbContext.WorkerHeartbeats
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var workers = heartbeats.Select(h => new WorkerHealthDto
        {
            WorkerName = h.WorkerName,
            LastHeartbeatUtc = h.LastHeartbeatUtc,
            Status = h.Status,
            LastErrorMessage = h.LastErrorMessage,
            IsAlive = (DateTimeOffset.UtcNow - h.LastHeartbeatUtc).TotalMinutes < 30
        }).ToList();

        var lastMarket = await _dbContext.Markets.AsNoTracking().OrderByDescending(m => m.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
        var lastAnalysis = await _dbContext.AiAnalyses.AsNoTracking().OrderByDescending(a => a.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
        var lastPrediction = await _dbContext.Predictions.AsNoTracking().OrderByDescending(p => p.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
        var lastEval = await _dbContext.Predictions.Where(p => p.EvaluatedAtUtc != null).AsNoTracking().OrderByDescending(p => p.EvaluatedAtUtc).FirstOrDefaultAsync(cancellationToken);

        return new SystemHealthDto
        {
            Workers = workers,
            LastMarketSyncUtc = lastMarket?.CreatedAtUtc,
            LastAnalysisUtc = lastAnalysis?.CreatedAtUtc,
            LastPredictionUtc = lastPrediction?.CreatedAtUtc,
            LastEvaluationUtc = lastEval?.EvaluatedAtUtc
        };
    }

    public async Task<SystemHealthSnapshot> CreateSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var activeMarkets = await _dbContext.Markets.CountAsync(m => m.Active, cancellationToken);
        var totalMarkets = await _dbContext.Markets.CountAsync(cancellationToken);
        var resolvedMarkets = await _dbContext.Markets.CountAsync(m => m.ResolvedAtUtc != null, cancellationToken);

        var totalAnalyses = await _dbContext.AiAnalyses.CountAsync(cancellationToken);
        var totalPredictions = await _dbContext.Predictions.CountAsync(cancellationToken);

        var queueStatus = await _queueMonitoringService.GetQueueStatusAsync(cancellationToken);
        var accuracyStatus = await _accuracyMonitoringService.GetAccuracyStatusAsync(cancellationToken);

        // Calculate Status
        var status = HealthStatus.Healthy;
        
        var deadWorkers = await _dbContext.WorkerHeartbeats
            .CountAsync(w => w.LastHeartbeatUtc < DateTimeOffset.UtcNow.AddMinutes(-30), cancellationToken);

        if (deadWorkers > 0 || queueStatus.FailedAnalyses >= 5)
        {
            status = HealthStatus.Warning;
        }
        
        var criticalWorkers = await _dbContext.WorkerHeartbeats
            .CountAsync(w => w.LastHeartbeatUtc < DateTimeOffset.UtcNow.AddHours(-2), cancellationToken);

        if (criticalWorkers > 0 || queueStatus.FailedAnalyses > 50)
        {
            status = HealthStatus.Critical;
        }

        var snapshot = new SystemHealthSnapshot
        {
            Id = Guid.NewGuid(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Status = status,
            TotalMarkets = totalMarkets,
            ActiveMarkets = activeMarkets,
            ResolvedMarkets = resolvedMarkets,
            QueuedMarkets = queueStatus.PendingAnalyses,
            ProcessingMarkets = queueStatus.ProcessingAnalyses,
            CompletedMarkets = queueStatus.CompletedAnalyses,
            FailedMarkets = queueStatus.FailedAnalyses,
            TotalAnalyses = totalAnalyses,
            TotalPredictions = totalPredictions,
            PredictionAccuracy = accuracyStatus.AccuracyPercentage,
            AverageBrierScore = accuracyStatus.AverageBrierScore,
            LastMarketCollectionUtc = await GetLastTimestampAsync<Market>(cancellationToken),
            LastAnalysisUtc = await GetLastTimestampAsync<AiAnalysis>(cancellationToken),
            LastPredictionUtc = await GetLastTimestampAsync<Prediction>(cancellationToken),
            LastEvaluationUtc = await _dbContext.Predictions.Where(p => p.EvaluatedAtUtc != null).MaxAsync(p => p.EvaluatedAtUtc, cancellationToken)
        };

        _dbContext.SystemHealthSnapshots.Add(snapshot);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Health Snapshot Created. Markets: {Markets}, Predictions: {Predictions}, Accuracy: {Accuracy}%, Avg Brier: {Brier}, Queue Pending: {Pending}, Queue Failed: {Failed}",
            totalMarkets, totalPredictions, accuracyStatus.AccuracyPercentage, accuracyStatus.AverageBrierScore, queueStatus.PendingAnalyses, queueStatus.FailedAnalyses);

        return snapshot;
    }

    public async Task<IReadOnlyList<SystemHealthSnapshot>> GetHistoricalSnapshotsAsync(int limit = 24, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SystemHealthSnapshots
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    private async Task<DateTimeOffset?> GetLastTimestampAsync<T>(CancellationToken cancellationToken) where T : BinaryPrediction.Core.Entities.Common.BaseEntity
    {
        return await _dbContext.Set<T>()
            .AsNoTracking()
            .OrderByDescending(e => e.CreatedAtUtc)
            .Select(e => e.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
