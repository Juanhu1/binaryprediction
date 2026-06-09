using BinaryPrediction.Core.DTOs;

namespace BinaryPrediction.Core.Interfaces;

public interface IOpenAiMonitoringService
{
    Task<OpenAiStatusDto> GetOpenAiStatusAsync(CancellationToken cancellationToken = default);
}
