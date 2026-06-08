using BinaryPrediction.Core.DTOs;

namespace BinaryPrediction.Core.Interfaces;

public interface IPredictionStatisticsService
{
    Task<PredictionAccuracySummary> GetAccuracySummaryAsync(CancellationToken cancellationToken = default);
    Task<List<ConfidenceBandResult>> GetConfidenceBandResultsAsync(CancellationToken cancellationToken = default);
    Task<int> GetTotalPredictionsAsync(CancellationToken cancellationToken = default);
    Task<int> GetResolvedPredictionsAsync(CancellationToken cancellationToken = default);
    Task<int> GetCorrectPredictionsAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetAccuracyAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetAverageBrierScoreAsync(CancellationToken cancellationToken = default);
}
