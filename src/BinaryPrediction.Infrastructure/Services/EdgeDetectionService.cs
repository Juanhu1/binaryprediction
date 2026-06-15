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
    private readonly IOpportunityLifecycleService _lifecycleService;
    private readonly EdgeDetectionOptions _options;
    private readonly ILogger<EdgeDetectionService> _logger;

    public EdgeDetectionService(
        IPredictionRepository predictionRepository,
        IPredictionOpportunityRepository opportunityRepository,
        IOpportunityLifecycleService lifecycleService,
        IOptions<EdgeDetectionOptions> options,
        ILogger<EdgeDetectionService> logger)
    {
        _predictionRepository = predictionRepository;
        _opportunityRepository = opportunityRepository;
        _lifecycleService = lifecycleService;
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

        var aiProb = prediction.PromptVersionUsed == "v2"
            ? prediction.AiProbability
            : (prediction.PredictedOutcome.Equals("Yes", StringComparison.OrdinalIgnoreCase)
                ? prediction.ConfidencePercentage
                : 100m - prediction.ConfidencePercentage);
        var marketProb = prediction.Market?.Probability ?? 0;
        var marketProbPct = marketProb * 100m;
        // Persist AI probability on the Prediction entity for admin UI
        prediction.AiProbability = aiProb;
        _logger.LogInformation("EDGE TEST: Market loaded. Probability={Probability} (pct={Pct})", marketProb, marketProbPct);
        var gap = Math.Abs(aiProb - marketProbPct);
        _logger.LogInformation("EDGE TEST: Gap={Gap}, Threshold={Threshold}", gap, _options.GapThresholdPercentage);
        var direction = aiProb > marketProbPct ? GapDirection.AIHigher : GapDirection.AILower;
        _logger.LogInformation("AI={AiProbability} Market={MarketProbabilityPct} Gap={Gap} Threshold={Threshold}", aiProb, marketProbPct, gap, _options.GapThresholdPercentage);
        var hasEdge = gap >= _options.GapThresholdPercentage;
        if (!hasEdge)
        {
            _logger.LogWarning("EDGE TEST: Opportunity not created. Reason={Reason}", "Gap below threshold");
        }

        var existing = await _opportunityRepository.GetByPredictionIdAsync(predictionId, cancellationToken);
        if (existing == null)
        {
            // Auto-expire older open/active opportunities for the same market when a new one is created
            var olderOpps = await _opportunityRepository.GetByMarketIdAsync(prediction.MarketId, cancellationToken);
            foreach (var olderOpp in olderOpps)
            {
                if (olderOpp.Status == OpportunityStatus.Open || olderOpp.Status == OpportunityStatus.Active)
                {
                    await _lifecycleService.ChangeStatusAsync(olderOpp.Id, OpportunityStatus.Expired, "Expired by newer opportunity creation", cancellationToken);
                    _logger.LogInformation("Edge detection: Auto-expired older opportunity {OpportunityId} for market {MarketId}", olderOpp.Id, prediction.MarketId);
                }
            }

            var opp = new PredictionOpportunity
            {
                PredictionId = predictionId,
                MarketId = prediction.MarketId,
                AiProbability = aiProb,
                MarketProbability = marketProbPct,
                ProbabilityGap = gap,
                GapDirection = direction,
                EdgeThresholdPercentage = _options.GapThresholdPercentage,
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
            existing.MarketProbability = marketProbPct;
            existing.ProbabilityGap = gap;
            existing.GapDirection = direction;
            existing.EdgeThresholdPercentage = _options.GapThresholdPercentage;
            existing.HasEdge = hasEdge;
            existing.DetectedAtUtc = DateTimeOffset.UtcNow;
            existing.LastStatusChangedAtUtc = DateTimeOffset.UtcNow;
            await _opportunityRepository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Edge detection: Updated opportunity for prediction {PredictionId} (Gap={Gap}, Edge={HasEdge}, Direction={Direction}).", predictionId, gap, hasEdge, direction);
        }
    }
}
