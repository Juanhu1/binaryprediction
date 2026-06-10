using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BinaryPrediction.Infrastructure.Persistence.Repositories;

public class PredictionRepository : IPredictionRepository
{
    private readonly BinaryPredictionDbContext _dbContext;

    public PredictionRepository(BinaryPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Prediction prediction, CancellationToken cancellationToken = default)
    {
        await _dbContext.Predictions.AddAsync(prediction, cancellationToken);
    }

    public async Task<IEnumerable<Prediction>> GetByMarketIdAsync(Guid marketId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Predictions
            .Where(p => p.MarketId == marketId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<Prediction?> GetLatestByMarketIdAsync(Guid marketId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Predictions
            .Where(p => p.MarketId == marketId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<Prediction>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Predictions
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task DeactivatePreviousPredictionsAsync(Guid marketId, CancellationToken cancellationToken = default)
    {
        var activePredictions = await _dbContext.Predictions
            .Where(p => p.MarketId == marketId && p.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var prediction in activePredictions)
        {
            prediction.IsActive = false;
            prediction.ExpiresAtUtc = DateTimeOffset.UtcNow;
        }
    }

    public async Task<bool> HasPredictionForAnalysisAsync(Guid analysisId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Predictions
            .AnyAsync(p => p.AnalysisId == analysisId, cancellationToken);
    }

    public async Task<IReadOnlyList<Prediction>> GetUnevaluatedPredictionsByMarketIdAsync(Guid marketId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Predictions
            .Where(p => p.MarketId == marketId && p.EvaluatedAtUtc == null)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Prediction>> GetEvaluatedPredictionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Predictions
            .Where(p => p.EvaluatedAtUtc != null)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountEvaluatedPredictionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Predictions
            .CountAsync(p => p.EvaluatedAtUtc != null, cancellationToken);
    }

    public async Task<int> CountCorrectPredictionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Predictions
            .CountAsync(p => p.WasCorrect == true, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    // Retrieves a prediction by its primary key
    public async Task<Prediction?> GetByIdAsync(Guid predictionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Predictions
            .FirstOrDefaultAsync(p => p.Id == predictionId, cancellationToken);
    }
}
