using BinaryPrediction.Core.DTOs;

namespace BinaryPrediction.Core.Interfaces;

public interface IHealthMonitoringService
{
    Task<SystemHealthDto> GetSystemHealthAsync(CancellationToken cancellationToken = default);
}
