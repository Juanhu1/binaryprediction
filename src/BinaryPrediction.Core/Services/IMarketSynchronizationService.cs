namespace BinaryPrediction.Core.Services;

public interface IMarketSynchronizationService
{
    Task SynchronizeActiveMarketsAsync(CancellationToken cancellationToken = default);
}
