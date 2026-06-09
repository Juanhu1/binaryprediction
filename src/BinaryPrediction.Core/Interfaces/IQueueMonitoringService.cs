using BinaryPrediction.Core.DTOs;

namespace BinaryPrediction.Core.Interfaces;

public interface IQueueMonitoringService
{
    Task<QueueStatusDto> GetQueueStatusAsync(CancellationToken cancellationToken = default);
}
