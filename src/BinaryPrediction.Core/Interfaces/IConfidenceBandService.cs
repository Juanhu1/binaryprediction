using BinaryPrediction.Core.DTOs;

namespace BinaryPrediction.Core.Interfaces;

public interface IConfidenceBandService
{
    Task<List<ConfidenceBandPerformanceDto>> GetConfidenceBandPerformanceAsync(CancellationToken cancellationToken = default);
}
