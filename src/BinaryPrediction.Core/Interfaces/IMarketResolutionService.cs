using BinaryPrediction.Core.Entities;

namespace BinaryPrediction.Core.Interfaces;

public interface IMarketResolutionService
{
    Task<IReadOnlyList<Market>> GetResolvedMarketsAsync(CancellationToken cancellationToken);
    Task<string?> GetActualOutcomeAsync(Market market, CancellationToken cancellationToken);
}
