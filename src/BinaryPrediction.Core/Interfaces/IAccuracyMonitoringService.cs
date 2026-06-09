using BinaryPrediction.Core.DTOs;

namespace BinaryPrediction.Core.Interfaces;

public interface IAccuracyMonitoringService
{
    Task<AccuracyStatusDto> GetAccuracyStatusAsync(CancellationToken cancellationToken = default);
}
