using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BinaryPrediction.Infrastructure.Services;

public class HealthMonitoringService : IHealthMonitoringService
{
    private readonly BinaryPredictionDbContext _dbContext;

    public HealthMonitoringService(BinaryPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SystemHealthDto> GetSystemHealthAsync(CancellationToken cancellationToken = default)
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
            // Consider worker alive if heartbeat is within last 5 minutes
            IsAlive = (DateTimeOffset.UtcNow - h.LastHeartbeatUtc).TotalMinutes < 5
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
}
