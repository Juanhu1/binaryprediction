using BinaryPrediction.Core.Common;
using BinaryPrediction.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace BinaryPrediction.Infrastructure.Services;

public class PredictionEvaluationService : IPredictionEvaluationService
{
    private readonly IPredictionRepository _predictionRepository;
    private readonly ILogger<PredictionEvaluationService> _logger;

    public PredictionEvaluationService(
        IPredictionRepository predictionRepository,
        ILogger<PredictionEvaluationService> logger)
    {
        _predictionRepository = predictionRepository;
        _logger = logger;
    }

    public async Task EvaluateMarketPredictionsAsync(Guid marketId, string actualOutcome, CancellationToken cancellationToken)
    {
        var normalizedActualOutcome = OutcomeNormalizer.Normalize(actualOutcome);

        if (normalizedActualOutcome == "Unknown")
        {
            _logger.LogWarning("Evaluation skipped for market {MarketId} because actual outcome is Unknown.", marketId);
            return;
        }

        var predictions = await _predictionRepository.GetUnevaluatedPredictionsByMarketIdAsync(marketId, cancellationToken);

        if (!predictions.Any())
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        foreach (var prediction in predictions)
        {
            var normalizedPredictedOutcome = OutcomeNormalizer.Normalize(prediction.PredictedOutcome);

            if (normalizedPredictedOutcome == "Unknown")
            {
                _logger.LogWarning("Prediction {PredictionId} has unknown outcome {Outcome}. Skipping.", prediction.Id, prediction.PredictedOutcome);
                continue;
            }

            prediction.ActualOutcome = normalizedActualOutcome;
            prediction.WasCorrect = normalizedPredictedOutcome == normalizedActualOutcome;
            
            // Calculate Brier Score
            // Assuming confidence is 0-100, convert to 0-1
            var confidenceProbability = prediction.ConfidenceScore / 100m;
            
            // For Brier Score: 
            // If predicted "Yes" with confidence P: outcome Yes -> (P - 1)^2, outcome No -> (P - 0)^2
            // If predicted "No" with confidence P: it's equal to predicting "Yes" with confidence (1 - P)
            var probabilityOfYes = normalizedPredictedOutcome == "Yes" ? confidenceProbability : (1m - confidenceProbability);
            
            var actualYesValue = normalizedActualOutcome == "Yes" ? 1m : 0m;

            prediction.BrierScore = (probabilityOfYes - actualYesValue) * (probabilityOfYes - actualYesValue);
            prediction.EvaluatedAtUtc = now;

            _logger.LogInformation("Prediction evaluated. MarketId: {MarketId}, PredictionId: {PredictionId}, PredictedOutcome: {PredictedOutcome}, ActualOutcome: {ActualOutcome}, Confidence: {Confidence}, WasCorrect: {WasCorrect}, BrierScore: {BrierScore}",
                marketId, prediction.Id, normalizedPredictedOutcome, normalizedActualOutcome, prediction.ConfidenceScore, prediction.WasCorrect, prediction.BrierScore);
        }

        await _predictionRepository.SaveChangesAsync(cancellationToken);
    }
}
