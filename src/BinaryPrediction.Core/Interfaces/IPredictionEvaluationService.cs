using BinaryPrediction.Core.Entities;

namespace BinaryPrediction.Core.Interfaces;

public interface IPredictionEvaluationService
{
    Task EvaluateMarketPredictionsAsync(Market market, string actualOutcome, CancellationToken cancellationToken);
}
