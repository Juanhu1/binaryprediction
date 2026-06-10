using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BinaryPrediction.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IPredictionOpportunityRepository"/>.
/// </summary>
public class PredictionOpportunityRepository : IPredictionOpportunityRepository
{
    private readonly BinaryPrediction.Infrastructure.Persistence.BinaryPredictionDbContext _dbContext;

    public PredictionOpportunityRepository(BinaryPrediction.Infrastructure.Persistence.BinaryPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PredictionOpportunity?> GetByPredictionIdAsync(Guid predictionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PredictionOpportunities
            .FirstOrDefaultAsync(o => o.PredictionId == predictionId, cancellationToken);
    }

    public async Task<IReadOnlyList<PredictionOpportunity>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.PredictionOpportunities
            .Where(o => o.HasEdge)
            .OrderByDescending(o => o.ProbabilityGap)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PredictionOpportunity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.PredictionOpportunities
            .OrderByDescending(o => o.ProbabilityGap)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PredictionOpportunity opportunity, CancellationToken cancellationToken = default)
    {
        await _dbContext.PredictionOpportunities.AddAsync(opportunity, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
