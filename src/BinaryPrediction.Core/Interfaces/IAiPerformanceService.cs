using BinaryPrediction.Core.DTOs;

namespace BinaryPrediction.Core.Interfaces;

public interface IAiPerformanceService
{
    Task<AiPerformanceDto> GetPerformanceAsync(CancellationToken cancellationToken = default);
}
