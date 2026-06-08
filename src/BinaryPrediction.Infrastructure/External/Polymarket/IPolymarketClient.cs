using BinaryPrediction.Infrastructure.External.Polymarket.DTOs;

namespace BinaryPrediction.Infrastructure.External.Polymarket;

public interface IPolymarketClient
{
    Task<IReadOnlyList<PolymarketMarketDto>> GetActiveMarketsAsync(CancellationToken cancellationToken);
    Task<PolymarketMarketDto?> GetMarketAsync(string slug, CancellationToken cancellationToken);
}
