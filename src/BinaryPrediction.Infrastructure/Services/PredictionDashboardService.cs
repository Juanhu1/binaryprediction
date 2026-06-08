using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace BinaryPrediction.Infrastructure.Services;

public class PredictionDashboardService : IPredictionDashboardService
{
    private readonly IPredictionPerformanceRepository _performanceRepository;
    private readonly IPredictionBenchmarkService _benchmarkService;
    private readonly IPredictionStatisticsService _statisticsService;
    private readonly ILogger<PredictionDashboardService> _logger;

    public PredictionDashboardService(
        IPredictionPerformanceRepository performanceRepository,
        IPredictionBenchmarkService benchmarkService,
        IPredictionStatisticsService statisticsService,
        ILogger<PredictionDashboardService> logger)
    {
        _performanceRepository = performanceRepository;
        _benchmarkService = benchmarkService;
        _statisticsService = statisticsService;
        _logger = logger;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var totalMarkets = await _performanceRepository.GetTotalMarketCountAsync(cancellationToken);
        var resolvedMarkets = await _performanceRepository.GetResolvedMarketCountAsync(cancellationToken);
        var totalPredictionsCount = await _statisticsService.GetTotalPredictionsAsync(cancellationToken);
        var evaluatedPredictionsCount = await _performanceRepository.GetEvaluatedPredictionCountAsync(cancellationToken);
        var accuracy = await _performanceRepository.GetAccuracyAsync(cancellationToken);
        var avgConf = await _performanceRepository.GetAverageConfidenceAsync(cancellationToken);
        var avgBrier = await _performanceRepository.GetAverageBrierScoreAsync(cancellationToken);

        var bands = await GetConfidenceBandsAsync(cancellationToken);
        
        var validBands = bands.Where(b => b.PredictionCount > 0).ToList();
        var bestBand = validBands.OrderByDescending(b => b.AccuracyPercentage).FirstOrDefault();
        var worstBand = validBands.OrderBy(b => b.AccuracyPercentage).FirstOrDefault();

        _logger.LogInformation("Performance dashboard generated. Accuracy: {Accuracy}%, Avg Confidence: {AvgConf}%",
            Math.Round(accuracy * 100, 2), Math.Round(avgConf, 2));

        return new DashboardSummaryDto
        {
            TotalMarkets = totalMarkets,
            ResolvedMarkets = resolvedMarkets,
            TotalPredictions = totalPredictionsCount,
            EvaluatedPredictions = evaluatedPredictionsCount,
            AccuracyPercentage = (decimal)(accuracy * 100),
            AverageConfidencePercentage = (decimal)avgConf,
            AverageBrierScore = (decimal)avgBrier,
            BestConfidenceBand = bestBand,
            WorstConfidenceBand = worstBand
        };
    }

    public async Task<List<AccuracyTrendDto>> GetDailyAccuracyTrendAsync(CancellationToken cancellationToken = default)
    {
        return await GetTrendAsync(p => p.EvaluatedAtUtc!.Value.Date.ToString("yyyy-MM-dd"), cancellationToken);
    }

    public async Task<List<AccuracyTrendDto>> GetWeeklyAccuracyTrendAsync(CancellationToken cancellationToken = default)
    {
        // ISO 8601 week is complex, but for simple weekly we can group by Year-Week
        // A simple approximation is grouping by the start of the week (Sunday or Monday)
        return await GetTrendAsync(p => 
        {
            var date = p.EvaluatedAtUtc!.Value.Date;
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).ToString("yyyy-MM-dd");
        }, cancellationToken);
    }

    public async Task<List<AccuracyTrendDto>> GetMonthlyAccuracyTrendAsync(CancellationToken cancellationToken = default)
    {
        return await GetTrendAsync(p => p.EvaluatedAtUtc!.Value.ToString("yyyy-MM"), cancellationToken);
    }

    private async Task<List<AccuracyTrendDto>> GetTrendAsync(Func<Prediction, string> groupKeySelector, CancellationToken cancellationToken)
    {
        var predictions = await _performanceRepository.GetEvaluatedPredictionsAsync(cancellationToken);

        var trends = predictions
            .GroupBy(groupKeySelector)
            .OrderBy(g => g.Key)
            .Select(g => new AccuracyTrendDto
            {
                Date = g.Key,
                PredictionCount = g.Count(),
                AccuracyPercentage = g.Any() ? (decimal)g.Count(p => p.WasCorrect == true) / g.Count() * 100m : 0,
                AverageBrierScore = g.Any() ? g.Average(p => p.BrierScore ?? 0) : 0
            })
            .ToList();

        _logger.LogInformation("Accuracy trend generated.");
        return trends;
    }

    public async Task<List<ConfidenceBandPerformanceDto>> GetConfidenceBandsAsync(CancellationToken cancellationToken = default)
    {
        var results = await _statisticsService.GetConfidenceBandResultsAsync(cancellationToken);
        
        _logger.LogInformation("Confidence calibration analysis completed.");

        return results.Select(r => new ConfidenceBandPerformanceDto
        {
            BandName = r.BandName,
            PredictionCount = r.TotalPredictions,
            CorrectCount = r.CorrectPredictions,
            AccuracyPercentage = r.AccuracyPercentage,
            AverageBrierScore = r.AverageBrierScore
        }).ToList();
    }

    public async Task<BenchmarkComparisonDto> GetBenchmarksAsync(CancellationToken cancellationToken = default)
    {
        return await _benchmarkService.CompareAllAsync(cancellationToken);
    }
}
