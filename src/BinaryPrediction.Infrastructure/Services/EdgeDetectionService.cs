using BinaryPrediction.Core.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BinaryPrediction.Infrastructure.Services;

/// <summary>
/// Detects prediction opportunities where the AI probability differs from the market probability
/// by a configured threshold.
/// </summary>
public class EdgeDetectionService : IEdgeDetectionService
{
    private readonly IPredictionRepository _predictionRepository;
    private readonly IPredictionOpportunityRepository _opportunityRepository;
    private readonly EdgeDetectionOptions _options;
    private readonly ILogger<EdgeDetectionService> _logger;

    public EdgeDetectionService(
        IPredictionRepository predictionRepository,
        IPredictionOpportunityRepository opportunityRepository,
        IOptions<EdgeDetectionOptions> options,
        ILogger<EdgeDetectionService> logger)
    {
        _predictionRepository = predictionRepository;
        _opportunityRepository = opportunityRepository;
        _options = options.Value;
        _logger = logger;
    }

    public async Task DetectOpportunityAsync(Guid predictionId, CancellationToken cancellationToken = default)
    {
        var prediction = await _predictionRepository.GetByIdAsync(predictionId, cancellationToken);
        if (prediction == null)
        {
            _logger.LogWarning("Edge detection: Prediction {PredictionId} not found.", predictionId);
            return;
        }

        // Only consider evaluated predictions
        if (prediction.EvaluatedAtUtc == null)
        {
            _logger.LogInformation("Edge detection: Prediction {PredictionId} not yet evaluated.", predictionId);
            return;
        }

        var aiProb = prediction.ConfidencePercentage; // AI confidence as percentage
        var marketProb = prediction.Market?.Probability ?? 0m; // Market odds probability
        var gap = Math.Abs(aiProb - marketProb);
        var hasEdge = gap >= _options.GapThresholdPercentage;

        var existing = await _opportunityRepository.GetByPredictionIdAsync(predictionId, cancellationToken);
        if (existing == null)
        {
            var opp = new PredictionOpportunity
            {
                PredictionId = predictionId,
                MarketId = prediction.MarketId,
                AiProbability = prediction.ConfidencePercentage,
                MarketProbability = prediction.Market?.Probability ?? 0m,
                ProbabilityGap = gap,
                HasEdge = hasEdge,
                DetectedAtUtc = DateTimeOffset.UtcNow
            };
            await _opportunityRepository.AddAsync(opp, cancellationToken);
            await _opportunityRepository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Edge detection: Created opportunity for prediction {PredictionId} (Gap={Gap}, Edge={HasEdge}).", predictionId, gap, hasEdge);
        }
        else
        {
            existing.AiProbability = prediction.ConfidencePercentage;
            existing.MarketProbability = prediction.Market?.Probability ?? 0m;
            existing.ProbabilityGap = gap;
            existing.HasEdge = hasEdge;
            existing.DetectedAtUtc = DateTimeOffset.UtcNow;
            await _opportunityRepository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Edge detection: Updated opportunity for prediction {PredictionId} (Gap={Gap}, Edge={HasEdge}).", predictionId, gap, hasEdge);
        }
    }
}
