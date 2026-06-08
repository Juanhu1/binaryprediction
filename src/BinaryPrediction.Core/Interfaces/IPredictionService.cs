using BinaryPrediction.Core.Entities;

namespace BinaryPrediction.Core.Interfaces;

public interface IPredictionService
{
    Task<Prediction?> CreatePredictionAsync(AiAnalysis analysis, Market market, CancellationToken cancellationToken = default);
    Task<Prediction?> GetLatestPredictionAsync(Guid marketId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Prediction>> GetActivePredictionsAsync(CancellationToken cancellationToken = default);
}
