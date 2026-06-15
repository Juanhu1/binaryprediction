using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BinaryPrediction.Infrastructure.Services;

public class PerformanceSnapshotService : IPerformanceSnapshotService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PerformanceSnapshotService> _logger;

    public PerformanceSnapshotService(IServiceScopeFactory scopeFactory, ILogger<PerformanceSnapshotService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task GenerateDailySnapshotAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BinaryPredictionDbContext>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        // Idempotency check
        if (await db.PredictionPerformanceSnapshots.AnyAsync(p => p.SnapshotDateUtc == today))
            return;

        var evaluated = db.Predictions.Where(p => p.EvaluatedAtUtc != null);
        var total = await evaluated.CountAsync();
        var correct = await evaluated.CountAsync(p => p.WasCorrect == true);
        var accuracy = total == 0 ? 0 : (decimal)correct / total * 100;
        var averageBrier = await evaluated
            .Where(p => p.BrierScore.HasValue)
            .Select(p => p.BrierScore.Value)
            .DefaultIfEmpty()
            .AverageAsync();

        var snapshot = new PredictionPerformanceSnapshot
        {
            Id = Guid.NewGuid(),
            SnapshotDateUtc = today,
            TotalPredictions = total,
            CorrectPredictions = correct,
            AccuracyPercentage = Math.Round(accuracy, 4),
            AverageBrierScore = Math.Round(averageBrier, 4),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        db.PredictionPerformanceSnapshots.Add(snapshot);
        await db.SaveChangesAsync();
    }

    public async Task GenerateCategorySnapshotsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BinaryPredictionDbContext>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var categories = await db.PredictionCategories.ToListAsync();
        foreach (var cat in categories)
        {
            // Category snapshot date handling
            // Use outer 'today' variable
            if (await db.CategoryPerformanceSnapshots.AnyAsync(c => c.PredictionCategoryId == cat.Id && c.SnapshotDateUtc == today))
                continue;

            var preds = await db.Predictions
                    .Include(p => p.Market)
                    .Where(p => p.Market != null && p.Market.PredictionCategoryId == cat.Id && p.WasCorrect != null)
                    .ToListAsync();
            var total = preds.Count;
            var correct = preds.Count(p => p.WasCorrect == true);
            var accuracy = total == 0 ? 0 : (decimal)correct / total * 100;
            var avgBrier = preds
                .Where(p => p.BrierScore.HasValue)
                .Select(p => p.BrierScore.Value)
                .DefaultIfEmpty()
                .Average();

            var snapshot = new CategoryPerformanceSnapshot
            {
                Id = Guid.NewGuid(),
                PredictionCategoryId = cat.Id,
                SnapshotDateUtc = today,
                PredictionCount = total,
                CorrectPredictions = correct,
                AccuracyPercentage = Math.Round(accuracy, 4),
                AverageBrierScore = avgBrier, // already rounded in assignment if needed
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            db.CategoryPerformanceSnapshots.Add(snapshot);
        }
        await db.SaveChangesAsync();
    }

    public async Task GenerateCalibrationSnapshotsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BinaryPredictionDbContext>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Define confidence buckets
        var buckets = new List<(int Min, int Max)>
        {
            (50,59),(60,69),(70,79),(80,89),(90,100)
        };

        foreach (var bucket in buckets)
        {
            var rangeLabel = $"{bucket.Min}-{bucket.Max}";
            if (await db.CalibrationTrendSnapshots.AnyAsync(c => c.ConfidenceRange == rangeLabel && c.SnapshotDateUtc == today))
                continue;

            var min = (decimal)bucket.Min;
            var max = (decimal)bucket.Max;
            var predictions = await db.Predictions
                .Where(p => p.EvaluatedAtUtc != null && p.ConfidencePercentage >= min && p.ConfidencePercentage <= max)
                .ToListAsync();
            var count = predictions.Count;
            if (count == 0) continue;

            var actualAcc = predictions.Count(p => p.WasCorrect == true);
            var actualPct = (decimal)actualAcc / count * 100;
            var expectedPct = predictions.Average(p => p.ConfidencePercentage);
            var error = Math.Abs(actualPct - expectedPct) / 100;

            var snapshot = new CalibrationTrendSnapshot
            {
                Id = Guid.NewGuid(),
                ConfidenceRange = rangeLabel,
                SnapshotDateUtc = today,
                PredictionCount = count,
                ActualAccuracyPercentage = Math.Round(actualPct, 4),
                ExpectedAccuracyPercentage = Math.Round(expectedPct, 4),
                CalibrationError = Math.Round(error, 4),
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            db.CalibrationTrendSnapshots.Add(snapshot);
        }
        await db.SaveChangesAsync();
}
// New method to rebuild all snapshots
    public async Task RebuildAllSnapshotsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BinaryPredictionDbContext>();
        try
        {
            _logger.LogInformation("Starting full analytics snapshot rebuild.");

            // 1. Delete existing snapshots
            db.PredictionPerformanceSnapshots.RemoveRange(db.PredictionPerformanceSnapshots);
            db.CategoryPerformanceSnapshots.RemoveRange(db.CategoryPerformanceSnapshots);
            db.CalibrationTrendSnapshots.RemoveRange(db.CalibrationTrendSnapshots);
            db.PredictionQualitySnapshots.RemoveRange(db.PredictionQualitySnapshots);
            db.OpportunityAnalyticsSnapshots.RemoveRange(db.OpportunityAnalyticsSnapshots);
            db.OpportunityLifecycleSnapshots.RemoveRange(db.OpportunityLifecycleSnapshots);
            await db.SaveChangesAsync();

            // 2. Get all distinct evaluation dates
            var dates = await db.Predictions
                .Where(p => p.EvaluatedAtUtc != null)
                .Select(p => DateOnly.FromDateTime(p.EvaluatedAtUtc.Value.UtcDateTime))
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

            // 3. Generate snapshots for each date
            foreach (var date in dates)
            {
                await GenerateSnapshotForDateAsync(db, date, enforceIdempotency: false);
            }

            _logger.LogInformation("Analytics snapshot rebuild completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analytics snapshot rebuild failed.");
            throw;
        }
    }

    // Internal helper to generate all three snapshot types for a specific date
    private async Task GenerateSnapshotForDateAsync(BinaryPredictionDbContext db, DateOnly date, bool enforceIdempotency)
    {
        if (enforceIdempotency && await db.PredictionPerformanceSnapshots.AnyAsync(p => p.SnapshotDateUtc == date))
            return;

        // ----- Performance Snapshot -----
        // ----- Performance Snapshot -----
        var perfEval = db.Predictions.Where(p => p.EvaluatedAtUtc != null && DateOnly.FromDateTime(p.EvaluatedAtUtc.Value.UtcDateTime) == date);
        var total = await perfEval.CountAsync();
        var correct = await perfEval.CountAsync(p => p.WasCorrect == true);
        var incorrect = await perfEval.CountAsync(p => p.WasCorrect != true); // false or null
        var accuracy = total == 0 ? 0 : (decimal)correct / total * 100;
        var avgError = await perfEval
            .Where(p => p.PredictionError.HasValue)
            .Select(p => p.PredictionError!.Value)
            .DefaultIfEmpty()
            .AverageAsync();

        var perfSnap = new PredictionPerformanceSnapshot
        {
            Id = Guid.NewGuid(),
            SnapshotDateUtc = date,
            TotalPredictions = total,
            CorrectPredictions = correct,
            IncorrectPredictions = incorrect,
            AccuracyPercentage = Math.Round(accuracy, 4),
            AveragePredictionError = Math.Round(avgError, 4),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        db.PredictionPerformanceSnapshots.Add(perfSnap);

        // ----- Category Snapshots -----
        var categories = await db.PredictionCategories.ToListAsync();
        foreach (var cat in categories)
        {
            var catPreds = await db.Predictions
                .Include(p => p.Market)
                .Where(p => p.Market != null && p.Market.PredictionCategoryId == cat.Id && p.WasCorrect != null && DateOnly.FromDateTime(p.EvaluatedAtUtc.Value.UtcDateTime) == date)
                .ToListAsync();
            var catTotal = catPreds.Count;
            var catCorrect = catPreds.Count(p => p.WasCorrect == true);
            var catAcc = catTotal == 0 ? 0 : (decimal)catCorrect / catTotal * 100;
            var catAvgBrier = catPreds.Where(p => p.BrierScore.HasValue).Select(p => p.BrierScore.Value).DefaultIfEmpty().Average();

            var catSnap = new CategoryPerformanceSnapshot
            {
                Id = Guid.NewGuid(),
                PredictionCategoryId = cat.Id,
                SnapshotDateUtc = date,
                PredictionCount = catTotal,
                CorrectPredictions = catCorrect,
                AccuracyPercentage = Math.Round(catAcc, 4),
                AverageBrierScore = Math.Round(catAvgBrier, 4),
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            db.CategoryPerformanceSnapshots.Add(catSnap);
        }

        // ----- Calibration Snapshots -----
        var buckets = new List<(int Min, int Max)>
        {
            (50, 59), (60, 69), (70, 79), (80, 89), (90, 100)
        };
        foreach (var bucket in buckets)
        {
            var rangeLabel = $"{bucket.Min}-{bucket.Max}";
            var min = (decimal)bucket.Min;
            var max = (decimal)bucket.Max;
            var preds = await db.Predictions
                .Where(p => p.EvaluatedAtUtc != null && DateOnly.FromDateTime(p.EvaluatedAtUtc.Value.UtcDateTime) == date && p.ConfidencePercentage >= min && p.ConfidencePercentage <= max)
                .ToListAsync();
            if (!preds.Any()) continue;
            var count = preds.Count;
            var actualAcc = preds.Count(p => p.WasCorrect == true);
            var actualPct = (decimal)actualAcc / count * 100;
            var expectedPct = preds.Average(p => p.ConfidencePercentage);
            var error = Math.Abs(actualPct - expectedPct) / 100;

            var calSnap = new CalibrationTrendSnapshot
            {
                Id = Guid.NewGuid(),
                ConfidenceRange = rangeLabel,
                SnapshotDateUtc = date,
                PredictionCount = count,
                ActualAccuracyPercentage = Math.Round(actualPct, 4),
                ExpectedAccuracyPercentage = Math.Round(expectedPct, 4),
                CalibrationError = Math.Round(error, 4),
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            db.CalibrationTrendSnapshots.Add(calSnap);
        }

        await db.SaveChangesAsync();
    }
}
