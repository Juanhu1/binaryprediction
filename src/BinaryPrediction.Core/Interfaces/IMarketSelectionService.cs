using BinaryPrediction.Core.Entities;

namespace BinaryPrediction.Core.Interfaces;

public interface IMarketSelectionService
{
    Task<List<Market>> GetEligibleMarketsAsync(CancellationToken cancellationToken = default);
}
