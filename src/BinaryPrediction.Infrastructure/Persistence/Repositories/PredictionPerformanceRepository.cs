using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BinaryPrediction.Infrastructure.Persistence.Repositories;

public class PredictionPerformanceRepository : IPredictionPerformanceRepository
{
    private readonly BinaryPredictionDbContext _dbContext;

    public PredictionPerformanceRepository(BinaryPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> GetTotalMarketCountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Markets
            .AsNoTracking()
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetResolvedMarketCountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Markets
            .AsNoTracking()
            .CountAsync(m => m.ActualOutcome != null && m.ResolvedAtUtc != null, cancellationToken);
    }

    public async Task<int> GetEvaluatedPredictionCountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Predictions
            .AsNoTracking()
            .CountAsync(p => p.EvaluatedAtUtc != null, cancellationToken);
    }

    public async Task<double> GetAccuracyAsync(CancellationToken cancellationToken = default)
    {
        var evaluatedCount = await GetEvaluatedPredictionCountAsync(cancellationToken);
        if (evaluatedCount == 0) return 0;

        var correctCount = await _dbContext.Predictions
            .AsNoTracking()
            .CountAsync(p => p.WasCorrect == true, cancellationToken);

        return (double)correctCount / evaluatedCount;
    }

    public async Task<double> GetAverageConfidenceAsync(CancellationToken cancellationToken = default)
    {
        var hasEvaluated = await _dbContext.Predictions
            .AsNoTracking()
            .AnyAsync(p => p.EvaluatedAtUtc != null, cancellationToken);

        if (!hasEvaluated) return 0;

        return (double)await _dbContext.Predictions
            .AsNoTracking()
            .Where(p => p.EvaluatedAtUtc != null)
            .AverageAsync(p => p.ConfidencePercentage, cancellationToken);
    }

    public async Task<double> GetAverageBrierScoreAsync(CancellationToken cancellationToken = default)
    {
        var hasEvaluated = await _dbContext.Predictions
            .AsNoTracking()
            .AnyAsync(p => p.EvaluatedAtUtc != null && p.BrierScore != null, cancellationToken);

        if (!hasEvaluated) return 0;

        return (double)await _dbContext.Predictions
            .AsNoTracking()
            .Where(p => p.EvaluatedAtUtc != null && p.BrierScore != null)
            .AverageAsync(p => p.BrierScore!.Value, cancellationToken);
    }

    public async Task<List<Prediction>> GetEvaluatedPredictionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Predictions
            .AsNoTracking()
            .Include(p => p.Market)
            .Where(p => p.EvaluatedAtUtc != null)
            .OrderBy(p => p.EvaluatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
