using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BinaryPrediction.Infrastructure.Repositories;

public class OpportunityStatusHistoryRepository : IOpportunityStatusHistoryRepository
{
    private readonly BinaryPrediction.Infrastructure.Persistence.BinaryPredictionDbContext _dbContext;

    public OpportunityStatusHistoryRepository(BinaryPrediction.Infrastructure.Persistence.BinaryPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(OpportunityStatusHistory history, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<OpportunityStatusHistory>().AddAsync(history, cancellationToken);
    }

    public async Task<IReadOnlyList<OpportunityStatusHistory>> GetByOpportunityIdAsync(Guid opportunityId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<OpportunityStatusHistory>()
            .Where(h => h.OpportunityId == opportunityId)
            .OrderByDescending(h => h.ChangedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
