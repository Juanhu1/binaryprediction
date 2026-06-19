using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using BinaryPrediction.Core.Common;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BinaryPrediction.Core.Enums;

namespace BinaryPrediction.Infrastructure.Services
{
    /// <summary>
    /// Service that repairs inconsistent evaluation data and recomputes edge detection opportunities.
    /// </summary>
    public class DataRepairService : IDataRepairService
    {
        private readonly BinaryPredictionDbContext _dbContext;
        private readonly ILogger<DataRepairService> _logger;
        private readonly IEdgeDetectionService _edgeDetectionService;
        private readonly IPredictionOpportunityRepository _opportunityRepo;
        private readonly IPredictionRepository _predictionRepo;
        private readonly IMarketEligibilityService _eligibilityService;
        private readonly IMarketQualityScoringService _scoringService;
        private readonly MarketFilteringSettings _settings;

        public DataRepairService(
            BinaryPredictionDbContext dbContext,
            ILogger<DataRepairService> logger,
            IEdgeDetectionService edgeDetectionService,
            IPredictionOpportunityRepository opportunityRepo,
            IPredictionRepository predictionRepo,
            IMarketEligibilityService eligibilityService,
            IMarketQualityScoringService scoringService,
            Microsoft.Extensions.Options.IOptions<MarketFilteringSettings> options)
        {
            _dbContext = dbContext;
            _logger = logger;
            _edgeDetectionService = edgeDetectionService;
            _opportunityRepo = opportunityRepo;
            _predictionRepo = predictionRepo;
            _eligibilityService = eligibilityService;
            _scoringService = scoringService;
            _settings = options.Value;
        }

        public async Task RepairAsync(CancellationToken cancellationToken = default)
        {
            var predictions = await _dbContext.Predictions
                .Include(p => p.Market)
                .Where(p => p.ActualOutcome != null && p.WasCorrect != null)
                .ToListAsync(cancellationToken);

            int repairedCount = 0;
            foreach (var prediction in predictions)
            {
                var actualOutcome = prediction.ActualOutcome?.Trim() ?? string.Empty;
                var predictedOutcome = prediction.PredictedOutcome?.Trim() ?? string.Empty;
                decimal predictedYesProbability;
                if (prediction.PromptVersionUsed == "v2")
                {
                    predictedYesProbability = prediction.AiProbability / 100m;
                }
                else
                {
                    var confidenceProbability = prediction.ConfidencePercentage / 100m;
                    predictedYesProbability = predictedOutcome.Equals("Yes", StringComparison.OrdinalIgnoreCase)
                        ? confidenceProbability
                        : (1m - confidenceProbability);
                }
                var actualYesValue = actualOutcome.Equals("Yes", StringComparison.OrdinalIgnoreCase) ? 1m : 0m;
                var correct = predictedOutcome.Equals(actualOutcome, StringComparison.OrdinalIgnoreCase);
                var error = Math.Abs(predictedYesProbability - actualYesValue);
                var brier = (predictedYesProbability - actualYesValue) * (predictedYesProbability - actualYesValue);

                bool needsUpdate = false;
                if (prediction.WasCorrect != correct)
                {
                    prediction.WasCorrect = correct;
                    needsUpdate = true;
                }
                if (prediction.PredictionError != error)
                {
                    prediction.PredictionError = error;
                    needsUpdate = true;
                }
                if (prediction.BrierScore != brier)
                {
                    prediction.BrierScore = brier;
                    needsUpdate = true;
                }
                if (needsUpdate)
                {
                    repairedCount++;
                }
            }

            if (repairedCount > 0)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Repaired {Count} prediction records with inconsistent evaluation data.", repairedCount);
            }
            else
            {
                _logger.LogInformation("No inconsistent prediction records found for repair.");
            }
        }

        public async Task RecomputeAllOpportunitiesAsync(CancellationToken cancellationToken = default)
        {
            // Delete all existing opportunity records
            await _opportunityRepo.DeleteAllAsync(cancellationToken);
            _logger.LogInformation("Deleted all existing PredictionOpportunity records.");

            // Get all active predictions (those with a market attached)
            var predictions = await _predictionRepo.GetActiveAsync(cancellationToken);
            foreach (var prediction in predictions)
            {
                try
                {
                    await _edgeDetectionService.DetectOpportunityAsync(prediction.Id, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error detecting opportunity for prediction {PredictionId}", prediction.Id);
                }
            }
            _logger.LogInformation("Recomputed opportunities for {Count} predictions.", predictions?.Count() ?? 0);
        }

        public async Task RepairOpportunitiesScaleAsync(CancellationToken cancellationToken = default)
        {
            var opportunities = await _dbContext.PredictionOpportunities.Include(o => o.Prediction).ToListAsync(cancellationToken);
            int updatedCount = 0;
            foreach (var o in opportunities)
            {
                bool updated = false;
                if (o.MarketProbability <= 1.0m)
                {
                    o.MarketProbability *= 100.0m;
                    updated = true;
                }
                if (o.EdgeThresholdPercentage == 0m)
                {
                    o.EdgeThresholdPercentage = 10.0m; // Default threshold percentage
                    updated = true;
                }
                
                var newGap = Math.Abs(o.AiProbability - o.MarketProbability);
                var newDirection = o.AiProbability > o.MarketProbability ? GapDirection.AIHigher : GapDirection.AILower;
                var newHasEdge = newGap >= o.EdgeThresholdPercentage;

                if (o.Prediction != null)
                {
                    if (o.ConfidencePercentage != o.Prediction.ConfidencePercentage)
                    {
                        o.ConfidencePercentage = o.Prediction.ConfidencePercentage;
                        updated = true;
                    }
                    var expectedEdgeScore = newGap * o.ConfidencePercentage;
                    if (o.EdgeScore != expectedEdgeScore)
                    {
                        o.EdgeScore = expectedEdgeScore;
                        updated = true;
                    }
                }

                if (o.ProbabilityGap != newGap || o.GapDirection != newDirection || o.HasEdge != newHasEdge || updated)
                {
                    o.ProbabilityGap = newGap;
                    o.GapDirection = newDirection;
                    o.HasEdge = newHasEdge;
                    updatedCount++;
                }
            }
            if (updatedCount > 0)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            _logger.LogInformation("Successfully normalized and recalculated {UpdatedCount} prediction opportunities out of {TotalCount} total opportunities.", updatedCount, opportunities.Count);
        }

        public async Task<string> RebuildMarketEligibilityAsync(CancellationToken cancellationToken = default)
        {
            var markets = await _dbContext.Markets.ToListAsync(cancellationToken);
            
            // Polymarket Stage Counts
            int polyTotal = 0;
            int polyActive = 0;
            int polyProbability = 0;
            int polyLiquidity = 0;
            int polyVolume = 0;
            int polyCategory = 0;
            int polyQuality = 0;
            int polyDuration = 0;

            // Kalshi Stage Counts
            int kalshiTotal = 0;
            int kalshiActive = 0;
            int kalshiProbability = 0;
            int kalshiParlay = 0;
            int kalshiVolume = 0;
            int kalshiCategory = 0;
            int kalshiQuality = 0;
            int kalshiDuration = 0;

            int updatedCount = 0;

            foreach (var market in markets)
            {
                bool marketChanged = false;

                // Fix Kalshi SourceUrl if it is using the old format
                if (market.MarketSource == MarketSource.Kalshi)
                {
                    var marketTicker = market.ExternalMarketId?.Trim();
                    var eventId = market.ExternalEventId?.Trim();
                    if (!string.IsNullOrEmpty(marketTicker))
                    {
                        var expectedUrl = GetKalshiSourceUrl(marketTicker, eventId);
                        if (market.SourceUrl != expectedUrl)
                        {
                            market.SourceUrl = expectedUrl;
                            marketChanged = true;
                        }
                    }
                }

                // Re-evaluate quality score and category under latest rules
                var (score, category, immediateRejection) = _scoringService.EvaluateMarketQuality(
                    market.Question, market.Liquidity, market.Volume, null, market.MarketSource);
                
                if (market.QualityScore != score)
                {
                    market.QualityScore = score;
                    marketChanged = true;
                }
                if (market.Category != category)
                {
                    market.Category = category;
                    marketChanged = true;
                }

                // Run actual eligibility evaluation to update in database
                var isEligible = _eligibilityService.EvaluateEligibility(market, out var reason);
                if (market.EligibleForAnalysis != isEligible || market.RejectionReason != reason)
                {
                    market.EligibleForAnalysis = isEligible;
                    market.RejectionReason = reason;
                    marketChanged = true;
                }

                if (marketChanged)
                {
                    updatedCount++;
                }

                // Sequential pipeline evaluation for counting
                if (market.MarketSource == MarketSource.Polymarket)
                {
                    polyTotal++;

                    // Stage 1: Active check
                    if (market.Active && !market.Closed)
                    {
                        polyActive++;

                        // Stage 2: Probability check
                        if (market.Probability > 0m && market.Probability < 1m)
                        {
                            polyProbability++;

                            // Stage 3: Liquidity check
                            if (market.Liquidity > 0m && market.Liquidity >= _settings.MinimumLiquidity)
                            {
                                polyLiquidity++;

                                // Stage 4: Volume check
                                if (market.Volume >= _settings.MinimumVolume)
                                {
                                    polyVolume++;

                                    // Stage 5: Category check
                                    if (_settings.EligibleCategories.Contains(market.Category))
                                    {
                                        polyCategory++;

                                        // Stage 6: Quality check
                                        if (market.QualityScore >= _settings.MinimumQualityScore)
                                        {
                                            polyQuality++;

                                            // Stage 7: Duration check
                                            var effectiveDate = market.EndDate ?? market.EstimatedResolutionDateUtc;
                                            if (effectiveDate.HasValue)
                                            {
                                                var maxDuration = TimeSpan.FromDays(_settings.MaximumMarketDurationDays);
                                                if (effectiveDate.Value - DateTimeOffset.UtcNow <= maxDuration)
                                                {
                                                    polyDuration++;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if (market.MarketSource == MarketSource.Kalshi)
                {
                    kalshiTotal++;

                    // Stage 1: Active check
                    if (market.Active && !market.Closed)
                    {
                        kalshiActive++;

                        // Stage 2: Probability check
                        if (market.Probability > 0m && market.Probability < 1m)
                        {
                            kalshiProbability++;

                            // Stage 3: Parlay check
                            var isParlay = false;
                            if (!string.IsNullOrEmpty(market.ExternalMarketId))
                            {
                                var extId = market.ExternalMarketId;
                                if (extId.StartsWith("KXMVESPORTSMULTIGAME", StringComparison.OrdinalIgnoreCase) ||
                                    extId.StartsWith("KXMVECROSSCATEGORY", StringComparison.OrdinalIgnoreCase))
                                {
                                    isParlay = true;
                                }
                            }
                            if (!isParlay)
                            {
                                kalshiParlay++;

                                // Stage 4: Volume check
                                var minVolume = _settings.KalshiMinimumVolume;
                                if (market.Volume > 0m && market.Volume >= minVolume)
                                {
                                    kalshiVolume++;

                                    // Stage 5: Category check
                                    if (_settings.EligibleCategories.Contains(market.Category))
                                    {
                                        kalshiCategory++;

                                        // Stage 6: Quality check
                                        if (market.QualityScore >= _settings.MinimumQualityScore)
                                        {
                                            kalshiQuality++;

                                            // Stage 7: Duration check
                                            var effectiveDate = market.EndDate ?? market.EstimatedResolutionDateUtc;
                                            if (effectiveDate.HasValue)
                                            {
                                                var maxDuration = TimeSpan.FromDays(_settings.MaximumMarketDurationDays);
                                                if (effectiveDate.Value - DateTimeOffset.UtcNow <= maxDuration)
                                                {
                                                    kalshiDuration++;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (updatedCount > 0)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            var summary = $@"Sequential Eligibility Pipeline Counts:
=== Polymarket ===
Stage 0 (Total): {polyTotal}
Stage 1 (Active & Open): {polyActive}
Stage 2 (Probability > 0 and < 1): {polyProbability}
Stage 3 (Liquidity > 0 and >= {_settings.MinimumLiquidity}): {polyLiquidity}
Stage 4 (Volume >= {_settings.MinimumVolume}): {polyVolume}
Stage 5 (Eligible Category): {polyCategory}
Stage 6 (Quality Score >= {_settings.MinimumQualityScore}): {polyQuality}
Stage 7 (Duration <= {_settings.MaximumMarketDurationDays} days): {polyDuration} (Final Eligible)

=== Kalshi ===
Stage 0 (Total): {kalshiTotal}
Stage 1 (Active & Open): {kalshiActive}
Stage 2 (Probability > 0 and < 1): {kalshiProbability}
Stage 3 (Exclude Parlays/Combos): {kalshiParlay}
Stage 4 (Volume > 0 and >= {_settings.KalshiMinimumVolume}): {kalshiVolume}
Stage 5 (Eligible Category): {kalshiCategory}
Stage 6 (Quality Score >= {_settings.MinimumQualityScore}): {kalshiQuality}
Stage 7 (Duration <= {_settings.MaximumMarketDurationDays} days): {kalshiDuration} (Final Eligible)

Eligibility rebuild updated {updatedCount} market records in database.";

            _logger.LogInformation(summary);
            return summary;
        }

        private static string GetKalshiSourceUrl(string marketTicker, string? eventId)
        {
            marketTicker = marketTicker.Trim();
            eventId = eventId?.Trim();

            string eventTicker;
            if (!string.IsNullOrEmpty(eventId))
            {
                if (eventId.Contains('-'))
                {
                    eventTicker = eventId;
                }
                else
                {
                    eventTicker = marketTicker;
                }
            }
            else
            {
                eventTicker = marketTicker;
            }

            var parts = eventTicker.Split('-');
            if (parts.Length > 2)
            {
                eventTicker = $"{parts[0]}-{parts[1]}";
            }

            string seriesTicker = parts[0];
            return $"https://kalshi.com/markets/{seriesTicker.ToLowerInvariant()}/{eventTicker.ToLowerInvariant()}";
        }
    }
}
