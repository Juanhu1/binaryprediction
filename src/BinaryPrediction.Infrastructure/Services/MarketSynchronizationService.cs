using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Repositories;
using BinaryPrediction.Core.Services;
using BinaryPrediction.Infrastructure.Persistence;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.External.Polymarket;
using BinaryPrediction.Infrastructure.External.Polymarket.DTOs;
using BinaryPrediction.Infrastructure.External.Kalshi;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using BinaryPrediction.Core.Options;
using BinaryPrediction.Core.Enums;

namespace BinaryPrediction.Infrastructure.Services;

public class MarketSynchronizationService : IMarketSynchronizationService
{
    private readonly IPolymarketClient _polymarketClient;
    private readonly IKalshiClient _kalshiClient;
    private readonly IRepository<Market> _marketRepository;
    private readonly IRepository<MarketSnapshot> _snapshotRepository;
    private readonly IMarketQuestionNormalizer _normalizer;
    private readonly IMarketQualityScoringService _scoringService;
    private readonly IMarketEligibilityService _eligibilityService;
    private readonly IMarketResolutionDateResolver _dateResolver;
    private readonly ILogger<MarketSynchronizationService> _logger;
    private readonly BinaryPredictionDbContext _dbContext;
    private readonly EdgeDetectionOptions _edgeOptions;

    public MarketSynchronizationService(
        IPolymarketClient polymarketClient,
        IKalshiClient kalshiClient,
        IRepository<Market> marketRepository,
        IRepository<MarketSnapshot> snapshotRepository,
        IMarketQuestionNormalizer normalizer,
        IMarketQualityScoringService scoringService,
        IMarketEligibilityService eligibilityService,
        IMarketResolutionDateResolver dateResolver,
        ILogger<MarketSynchronizationService> logger,
        BinaryPredictionDbContext dbContext,
        IOptions<EdgeDetectionOptions> edgeOptions)
    {
        _polymarketClient = polymarketClient;
        _kalshiClient = kalshiClient;
        _marketRepository = marketRepository;
        _snapshotRepository = snapshotRepository;
        _normalizer = normalizer;
        _scoringService = scoringService;
        _eligibilityService = eligibilityService;
        _dateResolver = dateResolver;
        _logger = logger;
        _dbContext = dbContext;
        _edgeOptions = edgeOptions.Value;
    }

    public async Task SynchronizeActiveMarketsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting market synchronization.");
        var polymarketMarkets = await _polymarketClient.GetActiveMarketsAsync(cancellationToken);
        var synchronizedCount = 0;
        var skippedCount = 0;

        foreach (var polymarketMarket in polymarketMarkets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!TryMapMarket(polymarketMarket, out var mappedMarket, out var probability))
            {
                skippedCount++;
                continue;
            }

            // Log raw market data received from Polymarket
            _logger.LogInformation("Market {Question} probability={Probability}", mappedMarket.Question, probability);

            var existingMarket = await _marketRepository.FirstOrDefaultAsync(
                market => market.MarketSource == MarketSource.Polymarket &&
                          ((market.ExternalMarketId != null && market.ExternalMarketId == mappedMarket.ExternalMarketId) ||
                           (market.ExternalMarketId == null && market.Slug == mappedMarket.Slug)),
                cancellationToken);

            var market = existingMarket ?? mappedMarket;
            if (existingMarket != null)
            {
                market.ExternalMarketId = mappedMarket.ExternalMarketId;
                market.SourceUrl = mappedMarket.SourceUrl;
            }
            
            // 1. Normalize
            market.Question = Truncate(_normalizer.Normalize(mappedMarket.Question), 500) ?? string.Empty;
            market.Active = mappedMarket.Active;
            market.Closed = mappedMarket.Closed;
            market.Liquidity = mappedMarket.Liquidity;
            market.Volume = mappedMarket.Volume;
            market.Probability = probability;
            
            // 2. Resolve Date
            var alternativeDate = polymarketMarket.CloseDate ?? polymarketMarket.EventDate ?? polymarketMarket.ResolveDate ?? polymarketMarket.GameDate ?? polymarketMarket.TournamentDate;
            var (resolvedDate, dateResolutionMethod) = _dateResolver.ResolveDate(polymarketMarket.Question, polymarketMarket.EndDate, alternativeDate);
            
            if (polymarketMarket.EndDate.HasValue && resolvedDate == polymarketMarket.EndDate)
            {
                market.EndDate = resolvedDate;
            }
            else
            {
                market.EstimatedResolutionDateUtc = resolvedDate;
            }
            
            _logger.LogInformation(dateResolutionMethod);

            // 3. Score & Classify
            var (score, category, immediateRejection) = _scoringService.EvaluateMarketQuality(
                market.Question, market.Liquidity, market.Volume, polymarketMarket.Tags, MarketSource.Polymarket);
            
            market.QualityScore = score;
            market.Category = category;
            // Ensure PredictionCategory exists and assign its Id
            var normalizedName = category.ToString().ToLower();
            var pc = await _dbContext.PredictionCategories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == normalizedName, cancellationToken);
            if (pc == null)
            {
                pc = new PredictionCategory { Id = Guid.NewGuid(), Name = category.ToString(), CreatedAtUtc = DateTimeOffset.UtcNow };
                _dbContext.PredictionCategories.Add(pc);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            market.PredictionCategoryId = pc.Id;
            market.LastQualityEvaluationUtc = DateTimeOffset.UtcNow;

            // 3. Eligibility
            if (immediateRejection != null)
            {
                market.EligibleForAnalysis = false;
                market.RejectionReason = immediateRejection;
            }
            else
            {
                var isEligible = _eligibilityService.EvaluateEligibility(market, out var reason);
                market.EligibleForAnalysis = isEligible;
                market.RejectionReason = reason;
            }

            _logger.LogInformation("Market evaluated: category={Category} score={Score} eligible={Eligible} reason={Reason}", 
                market.Category, market.QualityScore, market.EligibleForAnalysis, market.RejectionReason);

            // Ensure market probability is set before persisting changes
            market.Probability = probability;

            if (existingMarket is null)
            {
                await _marketRepository.AddAsync(market, cancellationToken);
            }
            else
            {
                await _marketRepository.UpdateAsync(market, cancellationToken);
            }

            // Update market probability with the latest snapshot value (redundant after above assignment)
            // market.Probability = probability;

            // Propagate updated market probability to any existing opportunities
            var opps = await _dbContext.PredictionOpportunities
                .Where(o => o.MarketId == market.Id)
                .ToListAsync(cancellationToken);

            foreach (var o in opps)
            {
                var marketProbPct = probability * 100m;
                o.MarketProbability = marketProbPct;
                o.ProbabilityGap = Math.Abs(o.AiProbability - o.MarketProbability);
                o.GapDirection = o.AiProbability > o.MarketProbability ? GapDirection.AIHigher : GapDirection.AILower;
                if (o.EdgeThresholdPercentage == 0m)
                {
                    o.EdgeThresholdPercentage = _edgeOptions.GapThresholdPercentage;
                }
                o.HasEdge = o.ProbabilityGap >= o.EdgeThresholdPercentage;
            }
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Insert snapshot (already below)
            await _snapshotRepository.AddAsync(new MarketSnapshot
            {
                MarketId = market.Id,
                Probability = probability,
                Liquidity = market.Liquidity
            }, cancellationToken);


            synchronizedCount++;
        }

        _logger.LogInformation(
            "Polymarket synchronization completed. Synced {SyncedCount} markets and skipped {SkippedCount}.",
            synchronizedCount,
            skippedCount);
    }

    private bool TryMapMarket(PolymarketMarketDto source, out Market market, out decimal probability)
    {
        market = new Market();
        probability = 0m;

        if (string.IsNullOrWhiteSpace(source.Question) || string.IsNullOrWhiteSpace(source.Slug))
        {
            _logger.LogDebug("Skipping Polymarket market with missing question or slug. Id: {PolymarketMarketId}", source.Id);
            return false;
        }

        if (source.Active != true || source.Closed == true)
        {
            _logger.LogDebug("Skipping inactive or closed Polymarket market {Slug}.", source.Slug);
            return false;
        }

        if (!TryGetYesProbability(source, out probability))
        {
            _logger.LogDebug("Skipping Polymarket market {Slug} because no usable probability was found.", source.Slug);
            return false;
        }

        market = new Market
        {
            MarketSource = MarketSource.Polymarket,
            ExternalMarketId = Truncate(source.Id, 200),
            SourceUrl = Truncate(source.Slug != null ? $"https://polymarket.com/market/{source.Slug}" : null, 500),
            Question = Truncate(source.Question.Trim(), 500) ?? string.Empty,
            Slug = Truncate(source.Slug.Trim(), 200) ?? string.Empty,
            Active = source.Active.GetValueOrDefault(),
            Closed = source.Closed.GetValueOrDefault(),
            Liquidity = source.Liquidity.GetValueOrDefault(),
            Volume = source.Volume.GetValueOrDefault()
        };

        return true;
    }

    private static bool TryGetYesProbability(PolymarketMarketDto source, out decimal probability)
    {
        probability = 0m;

        if (source.OutcomePrices is null || source.OutcomePrices.Count == 0)
        {
            return false;
        }

        var priceIndex = GetYesOutcomeIndex(source.Outcomes);
        if (priceIndex >= source.OutcomePrices.Count)
        {
            priceIndex = 0;
        }

        return decimal.TryParse(
            source.OutcomePrices[priceIndex],
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out probability);
    }

    private static int GetYesOutcomeIndex(IReadOnlyList<string>? outcomes)
    {
        if (outcomes is null)
        {
            return 0;
        }

        for (var index = 0; index < outcomes.Count; index++)
        {
            if (string.Equals(outcomes[index], "Yes", StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return 0;
    }

    public async Task SynchronizeKalshiMarketsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Kalshi market synchronization.");
        var kalshiMarkets = await _kalshiClient.GetActiveMarketsAsync(cancellationToken);
        var synchronizedCount = 0;
        var skippedCount = 0;

        foreach (var kalshiMarket in kalshiMarkets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!TryMapKalshiMarket(kalshiMarket, out var mappedMarket, out var probability))
            {
                skippedCount++;
                continue;
            }

            _logger.LogInformation("Kalshi Market {Question} probability={Probability}", mappedMarket.Question, probability);

            var existingMarket = await _marketRepository.FirstOrDefaultAsync(
                market => market.MarketSource == MarketSource.Kalshi &&
                          market.ExternalMarketId == mappedMarket.ExternalMarketId,
                cancellationToken);

            var market = existingMarket ?? mappedMarket;

            if (existingMarket != null)
            {
                market.SourceUrl = mappedMarket.SourceUrl;
                market.ExternalEventId = mappedMarket.ExternalEventId;
            }

            // 1. Normalize Question
            market.Question = Truncate(_normalizer.Normalize(mappedMarket.Question), 500) ?? string.Empty;
            market.Active = mappedMarket.Active;
            market.Closed = mappedMarket.Closed;
            market.Liquidity = mappedMarket.Liquidity;
            market.Volume = mappedMarket.Volume;
            market.Probability = probability;
            
            // 2. Resolve Date
            market.EndDate = mappedMarket.EndDate;

            // 3. Score & Classify
            var (score, category, immediateRejection) = _scoringService.EvaluateMarketQuality(
                market.Question, market.Liquidity, market.Volume, Array.Empty<string>(), MarketSource.Kalshi);
            
            market.QualityScore = score;
            market.Category = category;
            
            var normalizedName = category.ToString().ToLower();
            var pc = await _dbContext.PredictionCategories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == normalizedName, cancellationToken);
            if (pc == null)
            {
                pc = new PredictionCategory { Id = Guid.NewGuid(), Name = category.ToString(), CreatedAtUtc = DateTimeOffset.UtcNow };
                _dbContext.PredictionCategories.Add(pc);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            market.PredictionCategoryId = pc.Id;
            market.LastQualityEvaluationUtc = DateTimeOffset.UtcNow;

            // 3. Eligibility
            if (immediateRejection != null)
            {
                market.EligibleForAnalysis = false;
                market.RejectionReason = immediateRejection;
            }
            else
            {
                var isEligible = _eligibilityService.EvaluateEligibility(market, out var reason);
                market.EligibleForAnalysis = isEligible;
                market.RejectionReason = reason;
            }

            _logger.LogInformation("Kalshi Market evaluated: category={Category} score={Score} eligible={Eligible} reason={Reason}", 
                market.Category, market.QualityScore, market.EligibleForAnalysis, market.RejectionReason);

            market.Probability = probability;

            if (existingMarket is null)
            {
                await _marketRepository.AddAsync(market, cancellationToken);
            }
            else
            {
                await _marketRepository.UpdateAsync(market, cancellationToken);
            }

            // Propagate updated market probability to any existing opportunities
            var opps = await _dbContext.PredictionOpportunities
                .Where(o => o.MarketId == market.Id)
                .ToListAsync(cancellationToken);

            foreach (var o in opps)
            {
                var marketProbPct = probability * 100m;
                o.MarketProbability = marketProbPct;
                o.ProbabilityGap = Math.Abs(o.AiProbability - o.MarketProbability);
                o.GapDirection = o.AiProbability > o.MarketProbability ? GapDirection.AIHigher : GapDirection.AILower;
                if (o.EdgeThresholdPercentage == 0m)
                {
                    o.EdgeThresholdPercentage = _edgeOptions.GapThresholdPercentage;
                }
                o.HasEdge = o.ProbabilityGap >= o.EdgeThresholdPercentage;
            }
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Insert snapshot
            await _snapshotRepository.AddAsync(new MarketSnapshot
            {
                MarketId = market.Id,
                Probability = probability,
                Liquidity = market.Liquidity
            }, cancellationToken);

            synchronizedCount++;
        }

        _logger.LogInformation(
            "Kalshi synchronization completed. Synced {SyncedCount} markets and skipped {SkippedCount}.",
            synchronizedCount,
            skippedCount);
    }

    private bool TryMapKalshiMarket(KalshiMarketDto source, out Market market, out decimal probability)
    {
        market = new Market();
        probability = 0m;

        if (string.IsNullOrWhiteSpace(source.Title) || string.IsNullOrWhiteSpace(source.Ticker))
        {
            _logger.LogDebug("Skipping Kalshi market with missing title or ticker. Ticker: {Ticker}", source.Ticker);
            return false;
        }

        // Active: status is "open" or "active"
        var isActive = string.Equals(source.Status, "active", StringComparison.OrdinalIgnoreCase) || 
                       string.Equals(source.Status, "open", StringComparison.OrdinalIgnoreCase);
        
        var isClosed = string.Equals(source.Status, "closed", StringComparison.OrdinalIgnoreCase) || 
                       string.Equals(source.Status, "settled", StringComparison.OrdinalIgnoreCase);

        // We only ingest active markets
        if (!isActive || isClosed)
        {
            _logger.LogDebug("Skipping inactive or closed Kalshi market {Ticker}.", source.Ticker);
            return false;
        }

        // Probability calculation
        if (source.YesBidDollars.HasValue && source.YesAskDollars.HasValue)
        {
            probability = (source.YesBidDollars.Value + source.YesAskDollars.Value) / 2m;
        }
        else if (source.LastPriceDollars.HasValue)
        {
            probability = source.LastPriceDollars.Value;
        }
        else
        {
            _logger.LogDebug("Skipping Kalshi market {Ticker} because no usable pricing was found.", source.Ticker);
            return false;
        }

        probability = Math.Clamp(probability, 0m, 1m);

        DateTimeOffset? endDate = source.CloseTime ?? source.ExpirationTime ?? source.ExpectedExpirationTime;

        var slug = "kalshi-" + source.Ticker.ToLowerInvariant().Replace("/", "-").Replace(" ", "-");

        market = new Market
        {
            MarketSource = MarketSource.Kalshi,
            ExternalMarketId = Truncate(source.Ticker.Trim(), 200),
            ExternalEventId = Truncate(source.EventTicker?.Trim(), 200),
            SourceUrl = Truncate($"https://kalshi.com/markets/{source.Ticker}", 500),
            Question = Truncate(source.Title.Trim(), 500) ?? string.Empty,
            Slug = Truncate(slug, 200) ?? string.Empty,
            Active = true,
            Closed = false,
            Liquidity = source.LiquidityDollars.GetValueOrDefault(),
            Volume = source.VolumeFp.GetValueOrDefault(),
            EndDate = endDate
        };

        return true;
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (value == null) return null;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }
}
