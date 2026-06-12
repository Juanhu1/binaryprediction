using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Repositories;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BinaryPrediction.Infrastructure.Repositories;

public class PredictionPerformanceRepository : IPredictionPerformanceRepository
{
    private readonly BinaryPredictionDbContext _dbContext;

    public PredictionPerformanceRepository(BinaryPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Snapshot operations ---------------------------------------------------
    public async Task AddAsync(PredictionPerformanceSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        await _dbContext.PredictionPerformanceSnapshots.AddAsync(snapshot, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PredictionPerformanceSnapshot?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.PredictionPerformanceSnapshots
            .OrderByDescending(s => s.SnapshotDateUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PredictionPerformanceSnapshot>> GetTrendAsync(DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken = default)
    {
        var start = DateOnly.FromDateTime(startDateUtc);
        var end = DateOnly.FromDateTime(endDateUtc);
        return await _dbContext.PredictionPerformanceSnapshots
            .Where(s => s.SnapshotDateUtc >= start && s.SnapshotDateUtc <= end)
            .OrderBy(s => s.SnapshotDateUtc)
            .ToListAsync(cancellationToken);
    }

    // Evaluation queries ----------------------------------------------------
    public async Task<IReadOnlyList<Prediction>> GetUnevaluatedResolvedPredictionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Predictions
            .Where(p => p.ResolvedAtUtc != null && p.EvaluatedAtUtc == null)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetAccuracyAsync(DateTime? dateUtc = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Predictions.AsQueryable();
        if (dateUtc.HasValue)
        {
            var d = dateUtc.Value.Date;
            query = query.Where(p => p.EvaluatedAtUtc.HasValue && p.EvaluatedAtUtc.Value.UtcDateTime.Date == d);
        }
        else
        {
            query = query.Where(p => p.EvaluatedAtUtc != null);
        }

        var total = await query.CountAsync(cancellationToken);
        if (total == 0) return 0m;
        var correct = await query.CountAsync(p => p.WasCorrect == true, cancellationToken);
        return (decimal)correct / total * 100m;
    }

    public async Task<decimal> GetAverageConfidenceAsync(DateTime? dateUtc = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Predictions.AsQueryable();
        if (dateUtc.HasValue)
        {
            var d = dateUtc.Value.Date;
            query = query.Where(p => p.EvaluatedAtUtc.HasValue && p.EvaluatedAtUtc.Value.UtcDateTime.Date == d);
        }
        else
        {
            query = query.Where(p => p.EvaluatedAtUtc != null);
        }
        return await query.AverageAsync(p => p.ConfidencePercentage, cancellationToken);
    }

    public async Task<decimal> GetAverageBrierScoreAsync(DateTime? dateUtc = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Predictions.AsQueryable();
        if (dateUtc.HasValue)
        {
            var d = dateUtc.Value.Date;
            query = query.Where(p => p.EvaluatedAtUtc.HasValue && p.EvaluatedAtUtc.Value.UtcDateTime.Date == d);
        }
        else
        {
            query = query.Where(p => p.EvaluatedAtUtc != null);
        }
        return await query.AverageAsync(p => p.BrierScore ?? 0m, cancellationToken);
    }
}
