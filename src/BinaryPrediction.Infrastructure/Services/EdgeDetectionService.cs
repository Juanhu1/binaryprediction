using BinaryPrediction.Core.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Core.Enums;
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
        _logger.LogInformation("Edge detection started for prediction {PredictionId}", predictionId);
        _logger.LogInformation("EDGE TEST: Entered DetectOpportunityAsync for prediction {PredictionId}", predictionId);
        var prediction = await _predictionRepository.GetByIdAsync(predictionId, cancellationToken);
        _logger.LogInformation("Running edge detection for prediction {PredictionId}", predictionId);
        _logger.LogInformation("EDGE TEST: Prediction loaded. Confidence={Confidence}", prediction?.ConfidencePercentage ?? 0);
        if (prediction == null)
        {
            _logger.LogWarning("Edge detection: Prediction {PredictionId} not found.", predictionId);
            return;
        }

        var aiProb = prediction.ConfidencePercentage;
        var marketProb = prediction.Market?.Probability ?? 0;
        _logger.LogInformation("EDGE TEST: Market loaded. Probability={Probability}", marketProb);
        var gap = Math.Abs(aiProb - marketProb);
        _logger.LogInformation("EDGE TEST: Gap={Gap}, Threshold={Threshold}", gap, _options.GapThresholdPercentage);
        var direction = aiProb > marketProb ? GapDirection.AIHigher : GapDirection.AILower;
        _logger.LogInformation("AI={AiProbability} Market={MarketProbability} Gap={Gap} Threshold={Threshold}", aiProb, marketProb, gap, _options.GapThresholdPercentage);
        var hasEdge = gap >= _options.GapThresholdPercentage;
        if (!hasEdge)
        {
            _logger.LogWarning("EDGE TEST: Opportunity not created. Reason={Reason}", "Gap below threshold");
        }

        var existing = await _opportunityRepository.GetByPredictionIdAsync(predictionId, cancellationToken);
        if (existing == null)
        {
            var opp = new PredictionOpportunity
            {
                PredictionId = predictionId,
                MarketId = prediction.MarketId,
                AiProbability = aiProb,
                MarketProbability = marketProb,
                ProbabilityGap = gap,
                GapDirection = direction,
                HasEdge = hasEdge,
                DetectedAtUtc = DateTimeOffset.UtcNow,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                Status = OpportunityStatus.Open,
                LastStatusChangedAtUtc = DateTimeOffset.UtcNow
            };
            _logger.LogInformation("EDGE TEST: Creating opportunity record");
            _logger.LogInformation("Creating opportunity for prediction {PredictionId}", predictionId);
            await _opportunityRepository.AddAsync(opp, cancellationToken);
            await _opportunityRepository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Edge detection: Created opportunity for prediction {PredictionId} (Gap={Gap}, Edge={HasEdge}, Direction={Direction}).", predictionId, gap, hasEdge, direction);
            _logger.LogInformation("EDGE TEST: Opportunity saved successfully");
        }
        else
        {
            existing.AiProbability = aiProb;
            existing.MarketProbability = marketProb;
            existing.ProbabilityGap = gap;
            existing.GapDirection = direction;
            existing.HasEdge = hasEdge;
            existing.DetectedAtUtc = DateTimeOffset.UtcNow;
            existing.LastStatusChangedAtUtc = DateTimeOffset.UtcNow;
            await _opportunityRepository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Edge detection: Updated opportunity for prediction {PredictionId} (Gap={Gap}, Edge={HasEdge}, Direction={Direction}).", predictionId, gap, hasEdge, direction);
        }
    }
}
