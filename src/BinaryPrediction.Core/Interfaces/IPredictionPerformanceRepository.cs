using BinaryPrediction.Core.Entities;

namespace BinaryPrediction.Core.Interfaces;

public interface IPredictionPerformanceRepository
{
    Task<int> GetTotalMarketCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetResolvedMarketCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetEvaluatedPredictionCountAsync(CancellationToken cancellationToken = default);
    Task<double> GetAccuracyAsync(CancellationToken cancellationToken = default);
    Task<double> GetAverageConfidenceAsync(CancellationToken cancellationToken = default);
    Task<double> GetAverageBrierScoreAsync(CancellationToken cancellationToken = default);
    Task<List<Prediction>> GetEvaluatedPredictionsAsync(CancellationToken cancellationToken = default);
}
