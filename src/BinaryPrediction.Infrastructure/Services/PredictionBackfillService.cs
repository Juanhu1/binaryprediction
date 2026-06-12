using System;
using System.Threading;
using System.Threading.Tasks;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BinaryPrediction.Infrastructure.Services;

public class PredictionBackfillService : IPredictionBackfillService
{
    private readonly BinaryPredictionDbContext _dbContext;
    private readonly ILogger<PredictionBackfillService> _logger;
    private readonly PredictionEvaluationService _evalService;

    public PredictionBackfillService(BinaryPredictionDbContext dbContext, ILogger<PredictionBackfillService> logger, PredictionEvaluationService evalService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _evalService = evalService;
    }

    public async Task<int> BackfillAsync(int batchSize = 1000, CancellationToken ct = default)
    {
        var predictions = await _dbContext.Predictions
            .Where(p => p.EvaluatedAtUtc != null && (p.ActualOutcome == null || p.PredictionError == null))
            .Take(batchSize)
            .ToListAsync(ct);

        if (!predictions.Any())
        {
            _logger.LogInformation("No predictions require backfill.");
            return 0;
        }

        foreach (var pred in predictions)
        {
            var market = await _dbContext.Markets
                .FirstOrDefaultAsync(m => m.Id == pred.MarketId, ct);

            if (market?.ActualOutcome == null)
            {
                _logger.LogWarning("Market {MarketId} missing ActualOutcome; skipping prediction {PredictionId}.", pred.MarketId, pred.Id);
                continue;
            }

            var (brier, error, wasCorrect) = _evalService.CalculateMetrics(market.ActualOutcome, pred.ConfidencePercentage);
            pred.BrierScore = brier;
            pred.PredictionError = error;
            pred.WasCorrect = wasCorrect;
            pred.ActualOutcome = market.ActualOutcome;
        }

        var updated = await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Backfilled {Count} predictions.", predictions.Count);
        return updated;
    }
}
