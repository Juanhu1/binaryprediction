using BinaryPrediction.Core.Entities;

namespace BinaryPrediction.Core.Interfaces;

public interface IAiAnalysisService
{
    Task ProcessMarketAsync(Market market, CancellationToken cancellationToken = default);
}
