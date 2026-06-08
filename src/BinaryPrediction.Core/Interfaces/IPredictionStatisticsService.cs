using BinaryPrediction.Core.DTOs;

namespace BinaryPrediction.Core.Interfaces;

public interface IPredictionStatisticsService
{
    Task<PredictionAccuracySummaryDto> GetAccuracySummaryAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ConfidenceBucketAccuracyDto>> GetConfidenceBucketAccuracyAsync(CancellationToken cancellationToken);
}
