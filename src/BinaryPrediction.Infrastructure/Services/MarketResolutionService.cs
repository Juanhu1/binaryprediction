using BinaryPrediction.Core.Common;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.External.Polymarket;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BinaryPrediction.Infrastructure.Services;

public class MarketResolutionService : IMarketResolutionService
{
    private readonly BinaryPredictionDbContext _dbContext;
    private readonly IPolymarketClient _polymarketClient;
    private readonly ILogger<MarketResolutionService> _logger;
    private readonly OpenAiSettings _settings;

    public MarketResolutionService(
        BinaryPredictionDbContext dbContext,
        IPolymarketClient polymarketClient,
        ILogger<MarketResolutionService> logger,
        IOptions<OpenAiSettings> options)
    {
        _dbContext = dbContext;
        _polymarketClient = polymarketClient;
        _logger = logger;
        _settings = options.Value;
    }

    public async Task<IReadOnlyList<Market>> GetResolvedMarketsAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        
        // Find markets that have unevaluated predictions, and are potentially resolved
        var candidateMarkets = await _dbContext.Markets
            .Where(m => (m.Closed || m.EndDate <= now) && 
                        _dbContext.Predictions.Any(p => p.MarketId == m.Id && p.EvaluatedAtUtc == null))
            .ToListAsync(cancellationToken);

        var resolvedMarkets = new List<Market>();

        foreach (var market in candidateMarkets)
        {
            if (_settings.UseMockAnalysis)
            {
                resolvedMarkets.Add(market);
                continue;
            }

            var polyMarket = await _polymarketClient.GetMarketAsync(market.Slug, cancellationToken);
            if (polyMarket != null && polyMarket.Closed == true)
            {
                resolvedMarkets.Add(market);
            }
        }

        return resolvedMarkets;
    }

    public async Task<string?> GetActualOutcomeAsync(Market market, CancellationToken cancellationToken)
    {
        if (_settings.UseMockAnalysis)
        {
            // For mock mode, generate a random outcome that can be tested
            return Random.Shared.Next(0, 2) == 0 ? "Yes" : "No";
        }

        var polyMarket = await _polymarketClient.GetMarketAsync(market.Slug, cancellationToken);
        
        if (polyMarket == null || polyMarket.Closed != true)
        {
            return null;
        }

        if (polyMarket.Outcomes == null || polyMarket.OutcomePrices == null)
        {
            return null;
        }

        // The resolved winning outcome usually has a price of "1"
        for (var i = 0; i < polyMarket.OutcomePrices.Count; i++)
        {
            if (polyMarket.OutcomePrices[i] == "1" || polyMarket.OutcomePrices[i] == "1.0")
            {
                if (i < polyMarket.Outcomes.Count)
                {
                    return OutcomeNormalizer.Normalize(polyMarket.Outcomes[i]);
                }
            }
        }

        return null;
    }
}
