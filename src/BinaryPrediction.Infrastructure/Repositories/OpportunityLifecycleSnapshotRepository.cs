using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BinaryPrediction.Infrastructure.Repositories;

public class OpportunityLifecycleSnapshotRepository : IOpportunityLifecycleSnapshotRepository
{
    private readonly BinaryPrediction.Infrastructure.Persistence.BinaryPredictionDbContext _dbContext;

    public OpportunityLifecycleSnapshotRepository(BinaryPrediction.Infrastructure.Persistence.BinaryPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(OpportunityLifecycleSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        await _dbContext.OpportunityLifecycleSnapshots.AddAsync(snapshot, cancellationToken);
    }

    public async Task<IReadOnlyList<OpportunityLifecycleSnapshot>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.OpportunityLifecycleSnapshots
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
