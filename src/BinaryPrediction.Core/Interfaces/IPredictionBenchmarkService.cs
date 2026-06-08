using BinaryPrediction.Core.DTOs;

namespace BinaryPrediction.Core.Interfaces;

public interface IPredictionBenchmarkService
{
    Task<BenchmarkResultDto> EvaluateAlwaysYesAsync(CancellationToken cancellationToken = default);
    Task<BenchmarkResultDto> EvaluateAlwaysNoAsync(CancellationToken cancellationToken = default);
    Task<BenchmarkResultDto> EvaluateRandomAsync(CancellationToken cancellationToken = default);
    Task<BenchmarkResultDto> EvaluateAiAsync(CancellationToken cancellationToken = default);
    Task<BenchmarkComparisonDto> CompareAllAsync(CancellationToken cancellationToken = default);
}
