using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Interfaces;

namespace BinaryPrediction.Infrastructure.Services;

public class PredictionsImprovementService : IPredictionsImprovementService
{
    private readonly IConfidenceBandService _confidenceBandService;
    private readonly IMarketCategoryPerformanceService _categoryPerformanceService;
    private readonly IPredictionQualityService _qualityService;

    public PredictionsImprovementService(
        IConfidenceBandService confidenceBandService,
        IMarketCategoryPerformanceService categoryPerformanceService,
        IPredictionQualityService qualityService)
    {
        _confidenceBandService = confidenceBandService;
        _categoryPerformanceService = categoryPerformanceService;
        _qualityService = qualityService;
    }

    public async Task<List<ImprovementRecommendation>> GenerateRecommendationsAsync(CancellationToken cancellationToken = default)
    {
        var recommendations = new List<ImprovementRecommendation>();

        // 1. Confidence Band Analysis
        var bands = await _confidenceBandService.GetConfidenceBandPerformanceAsync(cancellationToken);
        
        var highConfidenceBand = bands.FirstOrDefault(b => b.BandName == "90-100");
        if (highConfidenceBand != null && highConfidenceBand.PredictionCount >= 5)
        {
            if (highConfidenceBand.AccuracyPercentage < 80m)
            {
                recommendations.Add(new ImprovementRecommendation
                {
                    Category = "Calibration",
                    Message = "Predictions above 90% confidence are overconfident. Consider reducing maximum confidence constraints.",
                    Severity = 2
                });
            }
        }

        var lowConfidenceBand = bands.FirstOrDefault(b => b.BandName == "50-59");
        if (lowConfidenceBand != null && lowConfidenceBand.PredictionCount >= 5)
        {
            if (lowConfidenceBand.AccuracyPercentage > 65m)
            {
                recommendations.Add(new ImprovementRecommendation
                {
                    Category = "Calibration",
                    Message = "Predictions in the 50-59% range are highly accurate. AI is underconfident in this range.",
                    Severity = 1
                });
            }
        }

        // 2. Category Performance Analysis
        var categories = await _categoryPerformanceService.GetCategoryPerformanceAsync(cancellationToken);
        if (categories.Any(c => c.PredictionCount >= 5))
        {
            var bestCategory = categories.Where(c => c.PredictionCount >= 5).OrderByDescending(c => c.AccuracyPercentage).First();
            var worstCategory = categories.Where(c => c.PredictionCount >= 5).OrderBy(c => c.AccuracyPercentage).First();

            if (bestCategory.AccuracyPercentage > 70m)
            {
                recommendations.Add(new ImprovementRecommendation
                {
                    Category = "Category Weights",
                    Message = $"{bestCategory.Category} markets outperform others ({Math.Round(bestCategory.AccuracyPercentage, 1)}%). Consider increasing weight for these predictions.",
                    Severity = 1
                });
            }

            if (worstCategory.AccuracyPercentage < 40m)
            {
                recommendations.Add(new ImprovementRecommendation
                {
                    Category = "Category Weights",
                    Message = $"{worstCategory.Category} markets have poor accuracy ({Math.Round(worstCategory.AccuracyPercentage, 1)}%). Review prompts for this category or exclude them.",
                    Severity = 3
                });
            }
        }

        // 3. Trend Analysis
        var trend = await _qualityService.CalculateImprovementTrendAsync(cancellationToken);
        if (trend < -5.0)
        {
            recommendations.Add(new ImprovementRecommendation
            {
                Category = "Trend",
                Message = $"Accuracy has deteriorated by {Math.Round(Math.Abs(trend), 1)}% over the last 30 days. Review recent prompting strategies.",
                Severity = 3
            });
        }

        // 4. Benchmark Advantage
        var advantage = await _qualityService.CalculateBenchmarkAdvantageAsync(cancellationToken);
        if (advantage < 0)
        {
            recommendations.Add(new ImprovementRecommendation
            {
                Category = "Benchmark",
                Message = $"AI is performing worse than random guessing by {Math.Round(Math.Abs(advantage), 1)}%. Critical calibration required.",
                Severity = 4
            });
        }

        return recommendations.OrderByDescending(r => r.Severity).ToList();
    }
}
