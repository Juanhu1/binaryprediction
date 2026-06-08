using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Interfaces;

namespace BinaryPrediction.Infrastructure.Services;

public class PredictionQualityService : IPredictionQualityService
{
    private readonly IPredictionPerformanceRepository _performanceRepository;
    private readonly IPredictionBenchmarkService _benchmarkService;

    public PredictionQualityService(
        IPredictionPerformanceRepository performanceRepository,
        IPredictionBenchmarkService benchmarkService)
    {
        _performanceRepository = performanceRepository;
        _benchmarkService = benchmarkService;
    }

    public async Task<PredictionQualityReport> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var accuracy = await _performanceRepository.GetAccuracyAsync(cancellationToken);
        var brier = await _performanceRepository.GetAverageBrierScoreAsync(cancellationToken);
        var calibration = await CalculateCalibrationErrorAsync(cancellationToken);
        var advantage = await CalculateBenchmarkAdvantageAsync(cancellationToken);
        var trend = await CalculateImprovementTrendAsync(cancellationToken);

        return new PredictionQualityReport
        {
            Accuracy = Math.Round(accuracy * 100, 2),
            AverageBrierScore = Math.Round(brier, 4),
            CalibrationError = Math.Round(calibration, 4),
            BenchmarkAdvantage = Math.Round(advantage, 2),
            ImprovementTrend = Math.Round(trend, 2)
        };
    }

    public async Task<double> CalculateCalibrationErrorAsync(CancellationToken cancellationToken = default)
    {
        var predictions = await _performanceRepository.GetEvaluatedPredictionsAsync(cancellationToken);
        if (!predictions.Any()) return 0;

        // Brier score is (forecast - actual)^2.
        // A simple calibration error is average(|forecast - actual outcome|).
        // Actual outcome is 1 for correct, 0 for incorrect relative to the predicted outcome?
        // Wait, if PredictedOutcome is "Yes" and Confidence is 80%, forecasted probability of Yes is 0.8.
        // If ActualOutcome is "Yes", actual is 1.0. Error is |0.8 - 1.0| = 0.2.
        // If ActualOutcome is "No", actual is 0.0. Error is |0.8 - 0.0| = 0.8.

        double totalError = 0;
        foreach (var p in predictions)
        {
            double forecast = (double)p.ConfidencePercentage / 100.0;
            double actual = p.WasCorrect == true ? 1.0 : 0.0;
            totalError += Math.Abs(forecast - actual);
        }

        return totalError / predictions.Count;
    }

    public async Task<double> CalculateImprovementTrendAsync(CancellationToken cancellationToken = default)
    {
        // 30 day moving average vs historical
        var predictions = await _performanceRepository.GetEvaluatedPredictionsAsync(cancellationToken);
        if (!predictions.Any()) return 0;

        var last30Days = predictions.Where(p => p.EvaluatedAtUtc >= DateTimeOffset.UtcNow.AddDays(-30)).ToList();
        var historical = predictions.Where(p => p.EvaluatedAtUtc < DateTimeOffset.UtcNow.AddDays(-30)).ToList();

        if (!historical.Any() || !last30Days.Any()) return 0;

        double recentAccuracy = (double)last30Days.Count(p => p.WasCorrect == true) / last30Days.Count * 100;
        double historicalAccuracy = (double)historical.Count(p => p.WasCorrect == true) / historical.Count * 100;

        return recentAccuracy - historicalAccuracy;
    }

    public async Task<double> CalculateBenchmarkAdvantageAsync(CancellationToken cancellationToken = default)
    {
        var aiBenchmark = await _benchmarkService.EvaluateAiAsync();
        var randomBenchmark = await _benchmarkService.EvaluateRandomAsync();

        // AI accuracy minus Random accuracy
        return (double)(aiBenchmark.AccuracyPercentage - randomBenchmark.AccuracyPercentage);
    }
}
