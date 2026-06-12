using BinaryPrediction.Core.Entities;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Logging;
using BinaryPrediction.Core.Common;
using System.Threading;
using System.Threading.Tasks;

namespace BinaryPrediction.Infrastructure.Services;

public class PredictionResolutionService : IPredictionResolutionService
{
    private readonly IPredictionResolutionRepository _resolutionRepository;
    private readonly BinaryPredictionDbContext _dbContext;
    private readonly ILogger<PredictionResolutionService> _logger;

    public PredictionResolutionService(IPredictionResolutionRepository resolutionRepository, BinaryPredictionDbContext dbContext, ILogger<PredictionResolutionService> logger)
    {
        _resolutionRepository = resolutionRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<int> ProcessPendingPredictionsAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _resolutionRepository.GetPendingPredictionsAsync(cancellationToken);
        if (pending == null || pending.Count == 0)
        {
            _logger.LogInformation("No pending predictions to process.");
            return 0;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var prediction in pending)
        {
            // Ensure market is loaded (Include already done in repository).
            var market = prediction.Market;
            if (market == null)
            {
                _logger.LogWarning("Prediction {PredictionId} has no market loaded; skipping.", prediction.Id);
                continue;
            }

            // Determine actual outcome (Yes/No) and its numeric value.
            var actualOutcomeNormalized = OutcomeNormalizer.Normalize(market.ActualOutcome ?? string.Empty);
            var actualYesValue = actualOutcomeNormalized.Equals("Yes", StringComparison.OrdinalIgnoreCase) ? 1m : 0m;

            // Determine predicted probability of Yes based on confidence.
            var confidenceProbability = prediction.ConfidencePercentage / 100m;
            var predictedYesProbability = confidenceProbability >= 0.5m ? confidenceProbability : (1m - confidenceProbability);

            // Calculate correctness.
            var predictedOutcome = confidenceProbability >= 0.5m ? "Yes" : "No";
            var wasCorrect = predictedOutcome.Equals(actualOutcomeNormalized, StringComparison.OrdinalIgnoreCase);

            // Brier score calculation.
            var brierScore = (predictedYesProbability - actualYesValue) * (predictedYesProbability - actualYesValue);

            // Populate fields.
            prediction.EvaluatedAtUtc = now;
            prediction.ResolvedAtUtc = market.ResolvedAtUtc;
            prediction.WasCorrect = wasCorrect;
            prediction.ActualOutcomeProbability = actualYesValue;
            if (prediction.BrierScore == null)
                prediction.BrierScore = brierScore;
            // Prediction error: absolute difference between predicted probability and actual outcome
            prediction.PredictionError = Math.Abs(predictedYesProbability - actualYesValue);
            prediction.ResolutionSource = "MarketResolutionService";

            // Idempotent history creation
            var exists = await _dbContext.PredictionResolutionHistories
                .AnyAsync(h => h.PredictionId == prediction.Id, cancellationToken);
            if (!exists)
            {
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
                await _dbContext.PredictionResolutionHistories.AddAsync(history, cancellationToken);
            }
        }

        await _resolutionRepository.SaveAsync(cancellationToken);
        _logger.LogInformation("Processed {Count} pending predictions.", pending.Count);
        return pending.Count;
    }
}
