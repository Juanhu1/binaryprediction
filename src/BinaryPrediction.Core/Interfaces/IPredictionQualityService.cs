using BinaryPrediction.Core.DTOs;

namespace BinaryPrediction.Core.Interfaces;

public interface IPredictionQualityService
{
    Task<PredictionQualityReport> GenerateAsync(CancellationToken cancellationToken = default);
    Task<double> CalculateCalibrationErrorAsync(CancellationToken cancellationToken = default);
    Task<double> CalculateImprovementTrendAsync(CancellationToken cancellationToken = default);
    Task<double> CalculateBenchmarkAdvantageAsync(CancellationToken cancellationToken = default);
}
