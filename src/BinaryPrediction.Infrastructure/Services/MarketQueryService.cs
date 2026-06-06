using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BinaryPrediction.Infrastructure.Services;

public class MarketQueryService : IMarketQueryService
{
    private readonly BinaryPredictionDbContext _dbContext;

    public MarketQueryService(BinaryPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<EligibleMarketView>> GetEligibleMarketsAsync(int limit, CancellationToken cancellationToken = default)
    {
        return await _dbContext.EligibleMarketsView
            .AsNoTracking()
            .OrderByDescending(m => m.QualityScore)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
