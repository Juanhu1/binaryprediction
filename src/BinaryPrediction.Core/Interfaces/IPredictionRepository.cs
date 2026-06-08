using BinaryPrediction.Core.Entities;

namespace BinaryPrediction.Core.Interfaces;

public interface IPredictionRepository
{
    Task AddAsync(Prediction prediction, CancellationToken cancellationToken = default);
    Task<IEnumerable<Prediction>> GetByMarketIdAsync(Guid marketId, CancellationToken cancellationToken = default);
    Task<Prediction?> GetLatestByMarketIdAsync(Guid marketId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Prediction>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task DeactivatePreviousPredictionsAsync(Guid marketId, CancellationToken cancellationToken = default);
    Task<bool> HasPredictionForAnalysisAsync(Guid analysisId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Prediction>> GetUnevaluatedPredictionsByMarketIdAsync(Guid marketId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Prediction>> GetEvaluatedPredictionsAsync(CancellationToken cancellationToken = default);
    Task<int> CountEvaluatedPredictionsAsync(CancellationToken cancellationToken = default);
    Task<int> CountCorrectPredictionsAsync(CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
