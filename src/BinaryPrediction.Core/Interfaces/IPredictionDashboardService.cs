using BinaryPrediction.Core.DTOs;

namespace BinaryPrediction.Core.Interfaces;

public interface IPredictionDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<List<AccuracyTrendDto>> GetDailyAccuracyTrendAsync(CancellationToken cancellationToken = default);
    Task<List<AccuracyTrendDto>> GetWeeklyAccuracyTrendAsync(CancellationToken cancellationToken = default);
    Task<List<AccuracyTrendDto>> GetMonthlyAccuracyTrendAsync(CancellationToken cancellationToken = default);
    Task<List<ConfidenceBandPerformanceDto>> GetConfidenceBandsAsync(CancellationToken cancellationToken = default);
    Task<BenchmarkComparisonDto> GetBenchmarksAsync(CancellationToken cancellationToken = default);
}
