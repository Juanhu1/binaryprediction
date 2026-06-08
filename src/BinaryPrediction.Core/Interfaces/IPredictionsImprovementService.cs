using BinaryPrediction.Core.DTOs;

namespace BinaryPrediction.Core.Interfaces;

public interface IPredictionsImprovementService
{
    Task<List<ImprovementRecommendation>> GenerateRecommendationsAsync(CancellationToken cancellationToken = default);
}
