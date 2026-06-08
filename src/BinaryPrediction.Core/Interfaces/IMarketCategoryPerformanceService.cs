using BinaryPrediction.Core.DTOs;

namespace BinaryPrediction.Core.Interfaces;

public interface IMarketCategoryPerformanceService
{
    Task<List<MarketCategoryPerformanceDto>> GetCategoryPerformanceAsync(CancellationToken cancellationToken = default);
}
