using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BinaryPrediction.Infrastructure.Services;

public class PredictionPerformanceService : IPredictionPerformanceService
{
    private readonly BinaryPredictionDbContext _dbContext;

    public PredictionPerformanceService(BinaryPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Generates a daily performance snapshot for the given UTC date (or today if null).
    /// </summary>
    public async Task GenerateDailySnapshotAsync(DateTime? snapshotDateUtc = null)
    {
        var dateTime = snapshotDateUtc?.Date ?? DateTime.UtcNow.Date;
        var date = DateOnly.FromDateTime(dateTime);

        // Prevent duplicate snapshots for the same date
        var exists = await _dbContext.PredictionPerformanceSnapshots
            .AnyAsync(s => s.SnapshotDateUtc == date);
        if (exists)
            return;

        var predictions = _dbContext.Predictions
            .Where(p => p.EvaluatedAtUtc.HasValue && DateOnly.FromDateTime(p.EvaluatedAtUtc.Value.UtcDateTime) == date);

        var total = await predictions.CountAsync();
        if (total == 0)
        {
            // No data for the day – still persist an empty snapshot for trend continuity.
            var emptySnapshot = new PredictionPerformanceSnapshot
            {
                SnapshotDateUtc = date,
                TotalPredictions = 0,
                CorrectPredictions = 0,
                IncorrectPredictions = 0,
                AccuracyPercentage = 0m,
                AverageConfidence = 0m,
                AverageBrierScore = 0m,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            };
            await _dbContext.PredictionPerformanceSnapshots.AddAsync(emptySnapshot);
            await _dbContext.SaveChangesAsync();
            return;
        }

        var correct = await predictions.CountAsync(p => p.WasCorrect == true);
        var incorrect = total - correct;
        var accuracy = total > 0 ? (decimal)correct / total * 100m : 0m;
        var avgConfidence = await predictions.AverageAsync(p => p.ConfidencePercentage);
        var avgBrier = await predictions.AverageAsync(p => p.BrierScore ?? 0m);
        var avgError = await predictions.AverageAsync(p => p.PredictionError ?? 0m);

        var snapshot = new PredictionPerformanceSnapshot
        {
            SnapshotDateUtc = date,
            TotalPredictions = total,
            CorrectPredictions = correct,
            IncorrectPredictions = incorrect,
            AccuracyPercentage = accuracy,
            AverageConfidence = avgConfidence,
            AverageBrierScore = avgBrier,
            AveragePredictionError = avgError,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        await _dbContext.PredictionPerformanceSnapshots.AddAsync(snapshot);
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Retrieves the most recent performance snapshot.
    /// </summary>
    public async Task<PredictionPerformanceSnapshot?> GetCurrentPerformanceAsync()
    {
        return await _dbContext.PredictionPerformanceSnapshots
            .OrderByDescending(s => s.SnapshotDateUtc)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Retrieves snapshots for the given date range (inclusive) ordered chronologically.
    /// </summary>
    public async Task<IReadOnlyList<PredictionPerformanceSnapshot>> GetPerformanceTrendAsync(DateTime startDateUtc, DateTime endDateUtc)
    {
        return await _dbContext.PredictionPerformanceSnapshots
            .Where(s => s.SnapshotDateUtc >= DateOnly.FromDateTime(startDateUtc) && s.SnapshotDateUtc <= DateOnly.FromDateTime(endDateUtc))
            .OrderBy(s => s.SnapshotDateUtc)
            .ToListAsync();
    }
}
