using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Enums;

namespace BinaryPrediction.Core.Interfaces;

public interface IMarketAnalysisQueueService
{
    Task EnqueueEligibleMarketsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MarketAnalysisQueueItem>> GetPendingItemsAsync(int limit, CancellationToken cancellationToken = default);
    Task MarkProcessingAsync(Guid queueItemId, CancellationToken cancellationToken = default);
    Task MarkCompletedAsync(Guid queueItemId, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(Guid queueItemId, string errorMessage, CancellationToken cancellationToken = default);
    Task RecoverStuckItemsAsync(CancellationToken cancellationToken = default);
    Task LogQueueStatusAsync(CancellationToken cancellationToken = default);
}
