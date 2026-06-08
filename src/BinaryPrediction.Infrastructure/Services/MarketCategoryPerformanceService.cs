using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BinaryPrediction.Infrastructure.Services;

public class MarketCategoryPerformanceService : IMarketCategoryPerformanceService
{
    private readonly BinaryPredictionDbContext _dbContext;

    public MarketCategoryPerformanceService(BinaryPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<MarketCategoryPerformanceDto>> GetCategoryPerformanceAsync(CancellationToken cancellationToken = default)
    {
        var evaluatedPredictions = await _dbContext.Predictions
            .Include(p => p.Market)
            .Where(p => p.EvaluatedAtUtc != null)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var categories = Enum.GetValues<BinaryPrediction.Core.Enums.MarketCategory>();

        var results = new List<MarketCategoryPerformanceDto>();

        foreach (var category in categories)
        {
            var categoryPredictions = evaluatedPredictions.Where(p => p.Market!.Category == category).ToList();

            var count = categoryPredictions.Count;
            var correct = categoryPredictions.Count(p => p.WasCorrect == true);
            var brierSum = categoryPredictions.Where(p => p.BrierScore.HasValue).Sum(p => p.BrierScore!.Value);
            var brierCount = categoryPredictions.Count(p => p.BrierScore.HasValue);

            results.Add(new MarketCategoryPerformanceDto
            {
                Category = category.ToString(),
                PredictionCount = count,
                AccuracyPercentage = count > 0 ? Math.Round((decimal)correct / count * 100, 2) : 0,
                AverageBrierScore = brierCount > 0 ? Math.Round(brierSum / brierCount, 4) : 0
            });
        }

        return results.OrderByDescending(r => r.PredictionCount).ToList();
    }
}
