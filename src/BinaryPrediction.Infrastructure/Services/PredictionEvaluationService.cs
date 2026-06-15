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

        var normalizedActual = actualOutcome?.Trim() ?? string.Empty;
        if (!normalizedActual.Equals("Yes", StringComparison.OrdinalIgnoreCase) && 
            !normalizedActual.Equals("No", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Unknown outcome '{Outcome}' for market {MarketId}; skipping evaluation.", actualOutcome, market.Id);
            return;
        }

        var now = DateTimeOffset.UtcNow;
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
        var metrics = CalculateMetricsForPrediction(actualOutcome, prediction);
        var wasCorrect = metrics.WasCorrect;
        var brierScore = metrics.BrierScore;
        var absoluteError = metrics.PredictionError;

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

    // Helper to compute metrics used by evaluation and back‑fill (legacy)
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

    // Overload helper to compute metrics for v1 or v2 predictions
    public (decimal BrierScore, decimal PredictionError, bool WasCorrect) CalculateMetricsForPrediction(string actualOutcome, Prediction prediction)
    {
        var normalizedActual = actualOutcome?.Trim() ?? string.Empty;
        var actualYesValue = normalizedActual.Equals("Yes", StringComparison.OrdinalIgnoreCase) ? 1m : 0m;

        decimal predictedYesProbability;
        string predictedOutcome;
        if (prediction.PromptVersionUsed == "v2")
        {
            predictedYesProbability = prediction.AiProbability / 100m;
            predictedOutcome = prediction.PredictedOutcome;
        }
        else
        {
            predictedOutcome = prediction.PredictedOutcome?.Trim() ?? string.Empty;
            var confidenceProbability = prediction.ConfidencePercentage / 100m;
            if (predictedOutcome.Equals("Yes", StringComparison.OrdinalIgnoreCase))
            {
                predictedYesProbability = confidenceProbability;
            }
            else if (predictedOutcome.Equals("No", StringComparison.OrdinalIgnoreCase))
            {
                predictedYesProbability = 1m - confidenceProbability;
            }
            else
            {
                predictedYesProbability = confidenceProbability >= 0.5m ? confidenceProbability : (1m - confidenceProbability);
                predictedOutcome = confidenceProbability >= 0.5m ? "Yes" : "No";
            }
        }

        var wasCorrect = predictedOutcome.Equals(normalizedActual, StringComparison.OrdinalIgnoreCase);
        var brierScore = (predictedYesProbability - actualYesValue) * (predictedYesProbability - actualYesValue);
        var predictionError = Math.Abs(predictedYesProbability - actualYesValue);
        return (brierScore, predictionError, wasCorrect);
    }

}
