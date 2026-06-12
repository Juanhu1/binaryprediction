using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BinaryPrediction.Infrastructure.Services;

public class PredictionEvaluationService : IPredictionEvaluationService
{
    private readonly BinaryPredictionDbContext _dbContext;
    private readonly ILogger<PredictionEvaluationService> _logger;

    public PredictionEvaluationService(BinaryPredictionDbContext dbContext, ILogger<PredictionEvaluationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task EvaluateMarketPredictionsAsync(Market market, string actualOutcome, CancellationToken cancellationToken = default)
    {
        if (market == null) throw new ArgumentNullException(nameof(market));
        if (string.IsNullOrWhiteSpace(actualOutcome))
        {
            _logger.LogWarning("Actual outcome is empty for market {MarketId}; skipping evaluation.", market.Id);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var normalizedActual = actualOutcome?.Trim() ?? string.Empty;
        var actualYesValue = normalizedActual.Equals("Yes", StringComparison.OrdinalIgnoreCase) ? 1m : 0m;

        var predictions = await _dbContext.Predictions
            .Where(p => p.MarketId == market.Id && p.EvaluatedAtUtc == null)
            .ToListAsync(cancellationToken);

        if (!predictions.Any())
        {
            _logger.LogInformation("No unevaluated predictions found for market {MarketId}.", market.Id);
            return;
        }

        foreach (var prediction in predictions)
        {
            EvaluateSinglePrediction(prediction, normalizedActual, actualYesValue, now, market);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Evaluated {Count} predictions for market {MarketId}.", predictions.Count, market.Id);
    }

    public void EvaluateSinglePrediction(Prediction prediction, string actualOutcome, decimal actualYesValue, DateTimeOffset evaluationTime, Market market)
    {
        var confidenceProbability = prediction.ConfidencePercentage / 100m;
        var predictedYesProbability = confidenceProbability >= 0.5m ? confidenceProbability : (1m - confidenceProbability);
        var predictedOutcome = confidenceProbability >= 0.5m ? "Yes" : "No";
        var wasCorrect = predictedOutcome.Equals(actualOutcome, StringComparison.OrdinalIgnoreCase);
        
        var brierScore = (predictedYesProbability - actualYesValue) * (predictedYesProbability - actualYesValue);
        var absoluteError = Math.Abs(predictedYesProbability - actualYesValue);

        prediction.BrierScore = brierScore;
        prediction.PredictionError = absoluteError;
        
        var history = new PredictionResolutionHistory
        {
            Id = Guid.NewGuid(),
            PredictionId = prediction.Id,
            MarketId = prediction.MarketId,
            ConfidencePercentage = prediction.ConfidencePercentage,
            ActualOutcome = market.ActualOutcome,
            WasCorrect = wasCorrect,
            BrierScore = brierScore,
            ResolvedAtUtc = market.ResolvedAtUtc,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        _dbContext.PredictionResolutionHistories.Add(history);
        
        prediction.ActualOutcome = actualOutcome;
        prediction.WasCorrect = wasCorrect;
        prediction.EvaluatedAtUtc = evaluationTime;
        prediction.ResolutionSource = "PredictionEvaluationService";
    }

    // Helper to compute metrics used by evaluation and back‑fill
    public (decimal BrierScore, decimal PredictionError, bool WasCorrect) CalculateMetrics(string actualOutcome, decimal confidencePercentage)
    {
        var normalizedActual = actualOutcome?.Trim() ?? string.Empty;
        var actualYesValue = normalizedActual.Equals("Yes", StringComparison.OrdinalIgnoreCase) ? 1m : 0m;
        var confidenceProbability = confidencePercentage / 100m;
        var predictedYesProbability = confidenceProbability >= 0.5m ? confidenceProbability : (1m - confidenceProbability);
        var predictedOutcome = confidenceProbability >= 0.5m ? "Yes" : "No";
        var wasCorrect = predictedOutcome.Equals(normalizedActual, StringComparison.OrdinalIgnoreCase);
        var brierScore = (predictedYesProbability - actualYesValue) * (predictedYesProbability - actualYesValue);
        var predictionError = Math.Abs(predictedYesProbability - actualYesValue);
        return (brierScore, predictionError, wasCorrect);
    }

}
