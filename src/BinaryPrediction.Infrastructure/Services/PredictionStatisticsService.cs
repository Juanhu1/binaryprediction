using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BinaryPrediction.Infrastructure.Services;

public class PredictionStatisticsService : IPredictionStatisticsService
{
    private readonly BinaryPredictionDbContext _dbContext;

    public PredictionStatisticsService(BinaryPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PredictionAccuracySummaryDto> GetAccuracySummaryAsync(CancellationToken cancellationToken)
    {
        var evaluatedPredictions = await _dbContext.Predictions
            .Where(p => p.EvaluatedAtUtc != null)
            .ToListAsync(cancellationToken);

        var total = evaluatedPredictions.Count;
        if (total == 0)
        {
            return new PredictionAccuracySummaryDto();
        }

        var correct = evaluatedPredictions.Count(p => p.WasCorrect == true);
        var incorrect = evaluatedPredictions.Count(p => p.WasCorrect == false);

        return new PredictionAccuracySummaryDto
        {
            TotalEvaluatedPredictions = total,
            CorrectPredictions = correct,
            IncorrectPredictions = incorrect,
            AccuracyPercentage = Math.Round((decimal)correct / total * 100, 2),
            AverageConfidence = Math.Round(evaluatedPredictions.Average(p => p.ConfidenceScore), 2),
            AverageBrierScore = evaluatedPredictions.Any(p => p.BrierScore != null) 
                ? Math.Round(evaluatedPredictions.Where(p => p.BrierScore != null).Average(p => p.BrierScore!.Value), 4)
                : null
        };
    }

    public async Task<IReadOnlyList<ConfidenceBucketAccuracyDto>> GetConfidenceBucketAccuracyAsync(CancellationToken cancellationToken)
    {
        var evaluatedPredictions = await _dbContext.Predictions
            .Where(p => p.EvaluatedAtUtc != null)
            .ToListAsync(cancellationToken);

        var buckets = new List<ConfidenceBucketAccuracyDto>();

        var ranges = new[]
        {
            (Min: 50m, Max: 60m, Label: "50-60"),
            (Min: 60m, Max: 70m, Label: "60-70"),
            (Min: 70m, Max: 80m, Label: "70-80"),
            (Min: 80m, Max: 90m, Label: "80-90"),
            (Min: 90m, Max: 100m, Label: "90-100")
        };

        foreach (var range in ranges)
        {
            var inBucket = evaluatedPredictions
                .Where(p => p.ConfidenceScore >= range.Min && p.ConfidenceScore <= range.Max) // using inclusive max as acceptable approximation, or > min, <= max
                .ToList();

            if (inBucket.Count == 0)
                continue;

            var correct = inBucket.Count(p => p.WasCorrect == true);

            buckets.Add(new ConfidenceBucketAccuracyDto
            {
                Bucket = range.Label,
                TotalPredictions = inBucket.Count,
                CorrectPredictions = correct,
                AccuracyPercentage = Math.Round((decimal)correct / inBucket.Count * 100, 2),
                AverageConfidence = Math.Round(inBucket.Average(p => p.ConfidenceScore), 2),
                AverageBrierScore = inBucket.Any(p => p.BrierScore != null)
                    ? Math.Round(inBucket.Where(p => p.BrierScore != null).Average(p => p.BrierScore!.Value), 4)
                    : null
            });
        }

        return buckets;
    }
}
