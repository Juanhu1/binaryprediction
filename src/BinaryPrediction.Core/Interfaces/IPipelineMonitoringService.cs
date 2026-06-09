using BinaryPrediction.Core.DTOs;

namespace BinaryPrediction.Core.Interfaces;

public interface IPipelineMonitoringService
{
    Task<PipelineStatusDto> GetPipelineStatusAsync(CancellationToken cancellationToken = default);
}
