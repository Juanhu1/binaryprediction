using BinaryPrediction.Core.Common;
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

    public async Task<PredictionAccuracySummary> GetAccuracySummaryAsync(CancellationToken cancellationToken = default)
    {
        return new PredictionAccuracySummary
        {
            TotalPredictions = await GetTotalPredictionsAsync(cancellationToken),
            ResolvedPredictions = await GetResolvedPredictionsAsync(cancellationToken),
            CorrectPredictions = await GetCorrectPredictionsAsync(cancellationToken),
            AccuracyPercentage = await GetAccuracyAsync(cancellationToken),
            AverageBrierScore = await GetAverageBrierScoreAsync(cancellationToken)
        };
    }

    public async Task<List<ConfidenceBandResult>> GetConfidenceBandResultsAsync(CancellationToken cancellationToken = default)
    {
        var evaluatedPredictions = await _dbContext.Predictions
            .Where(p => p.EvaluatedAtUtc != null)
            .ToListAsync(cancellationToken);

        var bands = evaluatedPredictions
            .GroupBy(p => PredictionConfidenceBand.GetBand(p.ConfidencePercentage))
            .OrderByDescending(g => g.Key)
            .Select(g => new ConfidenceBandResult
            {
                BandName = g.Key,
                TotalPredictions = g.Count(),
                CorrectPredictions = g.Count(p => p.WasCorrect == true),
                AccuracyPercentage = g.Any() ? Math.Round((decimal)g.Count(p => p.WasCorrect == true) / g.Count() * 100, 2) : 0,
                AverageBrierScore = g.Any(p => p.BrierScore.HasValue) ? Math.Round(g.Where(p => p.BrierScore.HasValue).Average(p => p.BrierScore!.Value), 4) : 0
            })
            .ToList();

        // Ensure all bands exist even if empty
        var expectedBands = new[] { "90-100", "80-89", "70-79", "60-69", "50-59" };
        foreach (var expectedBand in expectedBands)
        {
            if (!bands.Any(b => b.BandName == expectedBand))
            {
                bands.Add(new ConfidenceBandResult
                {
                    BandName = expectedBand,
                    TotalPredictions = 0,
                    CorrectPredictions = 0,
                    AccuracyPercentage = 0,
                    AverageBrierScore = 0
                });
            }
        }

        return bands.OrderByDescending(b => b.BandName).ToList();
    }

    public async Task<int> GetTotalPredictionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Predictions.CountAsync(cancellationToken);
    }

    public async Task<int> GetResolvedPredictionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Predictions.CountAsync(p => p.EvaluatedAtUtc != null, cancellationToken);
    }

    public async Task<int> GetCorrectPredictionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Predictions.CountAsync(p => p.WasCorrect == true, cancellationToken);
    }

    public async Task<decimal> GetAccuracyAsync(CancellationToken cancellationToken = default)
    {
        var resolved = await GetResolvedPredictionsAsync(cancellationToken);
        if (resolved == 0) return 0;
        
        var correct = await GetCorrectPredictionsAsync(cancellationToken);
        return Math.Round((decimal)correct / resolved * 100, 2);
    }

    public async Task<decimal> GetAverageBrierScoreAsync(CancellationToken cancellationToken = default)
    {
        var brierScores = await _dbContext.Predictions
            .Where(p => p.BrierScore != null)
            .Select(p => p.BrierScore!.Value)
            .ToListAsync(cancellationToken);

        if (!brierScores.Any()) return 0;
        return Math.Round(brierScores.Average(), 4);
    }
}
