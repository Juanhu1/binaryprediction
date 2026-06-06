using BinaryPrediction.Core.Entities;

namespace BinaryPrediction.Core.Interfaces;

public interface IMarketQueryService
{
    Task<IReadOnlyList<EligibleMarketView>> GetEligibleMarketsAsync(int limit, CancellationToken cancellationToken = default);
}
