using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Infrastructure.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace BinaryPrediction.Infrastructure.Services
{
    public class MarketIntegrityChecker : IMarketIntegrityChecker
    {
        private readonly BinaryPredictionDbContext _dbContext;
        private readonly ILogger<MarketIntegrityChecker> _logger;

        public MarketIntegrityChecker(BinaryPredictionDbContext dbContext, ILogger<MarketIntegrityChecker> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<IReadOnlyList<Market>> GetInvalidMarketsAsync()
        {
            var invalidMarkets = await _dbContext.Markets
                .Where(m => m.ResolvedAtUtc != null && m.EndDate != null && m.ResolvedAtUtc < m.EndDate)
                .ToListAsync();

            foreach (var market in invalidMarkets)
            {
                _logger.LogWarning("Invalid market detected: MarketId={MarketId}, Question={Question}, EndDate={EndDate}, ResolvedAtUtc={ResolvedAtUtc}",
                    market.Id, market.Question, market.EndDate, market.ResolvedAtUtc);
            }

            return invalidMarkets;
        }
    }
}
