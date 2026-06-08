namespace BinaryPrediction.Core.Interfaces;

public interface IPredictionEvaluationService
{
    Task EvaluateMarketPredictionsAsync(Guid marketId, string actualOutcome, CancellationToken cancellationToken);
}
