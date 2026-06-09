using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Entities;

namespace BinaryPrediction.Core.Interfaces;

public interface ISystemHealthService
{
    Task<SystemHealthDto> GetCurrentHealthAsync(CancellationToken cancellationToken = default);
    Task<SystemHealthSnapshot> CreateSnapshotAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SystemHealthSnapshot>> GetHistoricalSnapshotsAsync(int limit = 24, CancellationToken cancellationToken = default);
}
