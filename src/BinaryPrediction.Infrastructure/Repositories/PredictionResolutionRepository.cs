using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BinaryPrediction.Infrastructure.Repositories;

public class PredictionResolutionRepository : IPredictionResolutionRepository
{
    private readonly BinaryPredictionDbContext _dbContext;

    public PredictionResolutionRepository(BinaryPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Prediction>> GetPendingPredictionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Predictions
            .Include(p => p.Market)
            .Where(p => p.EvaluatedAtUtc == null && p.Market != null && p.Market.ResolvedAtUtc != null)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Predictions
            .Where(p => p.EvaluatedAtUtc == null && p.Market != null && p.Market.ResolvedAtUtc != null)
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetResolvedCountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Predictions
            .Where(p => p.EvaluatedAtUtc != null)
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetResolvedTodayAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTimeOffset.UtcNow.Date;
        return await _dbContext.Predictions
            .Where(p => p.Market != null && p.Market.ResolvedAtUtc.HasValue && p.Market.ResolvedAtUtc.Value.UtcDateTime.Date == today)
            .CountAsync(cancellationToken);
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
