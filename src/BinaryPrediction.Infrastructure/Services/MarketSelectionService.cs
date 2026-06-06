using BinaryPrediction.Core.Common;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BinaryPrediction.Infrastructure.Services;

public class MarketSelectionService : IMarketSelectionService
{
    private readonly BinaryPredictionDbContext _dbContext;
    private readonly MarketFilteringSettings _settings;

    public MarketSelectionService(BinaryPredictionDbContext dbContext, IOptions<MarketFilteringSettings> options)
    {
        _dbContext = dbContext;
        _settings = options.Value;
    }

    public async Task<List<Market>> GetEligibleMarketsAsync(CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTimeOffset.UtcNow.AddMinutes(-_settings.ReanalysisCooldownMinutes);

        return await _dbContext.Set<Market>()
            .Where(m => m.EligibleForAnalysis)
            .Where(m => !_dbContext.Set<AiAnalysis>()
                .Any(a => a.MarketId == m.Id && a.CreatedAtUtc > cutoffTime))
            .Take(_settings.MaxMarketsPerCycle)
            .ToListAsync(cancellationToken);
    }
}
