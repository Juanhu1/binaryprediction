using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Repositories;
using BinaryPrediction.Core.Services;
using BinaryPrediction.Infrastructure.Persistence;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.External.Polymarket;
using BinaryPrediction.Infrastructure.External.Polymarket.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace BinaryPrediction.Infrastructure.Services;

public class MarketSynchronizationService : IMarketSynchronizationService
{
    private readonly IPolymarketClient _polymarketClient;
    private readonly IRepository<Market> _marketRepository;
    private readonly IRepository<MarketSnapshot> _snapshotRepository;
    private readonly IMarketQuestionNormalizer _normalizer;
    private readonly IMarketQualityScoringService _scoringService;
    private readonly IMarketEligibilityService _eligibilityService;
    private readonly IMarketResolutionDateResolver _dateResolver;
    private readonly ILogger<MarketSynchronizationService> _logger;
        private readonly BinaryPredictionDbContext _dbContext;

    public MarketSynchronizationService(
        IPolymarketClient polymarketClient,
        IRepository<Market> marketRepository,
        IRepository<MarketSnapshot> snapshotRepository,
        IMarketQuestionNormalizer normalizer,
        IMarketQualityScoringService scoringService,
        IMarketEligibilityService eligibilityService,
        IMarketResolutionDateResolver dateResolver,
        ILogger<MarketSynchronizationService> logger,
        BinaryPredictionDbContext dbContext)
    {
        _polymarketClient = polymarketClient;
        _marketRepository = marketRepository;
        _snapshotRepository = snapshotRepository;
        _normalizer = normalizer;
        _scoringService = scoringService;
        _eligibilityService = eligibilityService;
        _dateResolver = dateResolver;
        _logger = logger;
        _dbContext = dbContext;
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
                market => market.Slug == mappedMarket.Slug,
                cancellationToken);

            var market = existingMarket ?? mappedMarket;
            
            // 1. Normalize
            market.Question = _normalizer.Normalize(mappedMarket.Question);
            market.Active = mappedMarket.Active;
            market.Closed = mappedMarket.Closed;
            market.Liquidity = mappedMarket.Liquidity;
            market.Volume = mappedMarket.Volume;
            
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
                market.Question, market.Liquidity, market.Volume, polymarketMarket.Tags);
            
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

            if (existingMarket is null)
            {
                await _marketRepository.AddAsync(market, cancellationToken);
            }
            else
            {
                await _marketRepository.UpdateAsync(market, cancellationToken);
            }

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
            Question = source.Question.Trim(),
            Slug = source.Slug.Trim(),
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
}
