using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BinaryPrediction.Worker.Jobs;

public class PredictionEvaluationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PredictionEvaluationWorker> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    public PredictionEvaluationWorker(
        IServiceProvider serviceProvider,
        ILogger<PredictionEvaluationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        _logger.LogInformation("PredictionEvaluationWorker starting with interval {Interval}.", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<BinaryPredictionDbContext>();
                var evaluationService = scope.ServiceProvider.GetRequiredService<IPredictionEvaluationService>();
                var statisticsService = scope.ServiceProvider.GetRequiredService<IPredictionStatisticsService>();
                var heartbeatService = scope.ServiceProvider.GetRequiredService<IWorkerHeartbeatService>();

                await heartbeatService.LogHeartbeatAsync(nameof(PredictionEvaluationWorker), "Processing", null, stoppingToken);

                _logger.LogInformation("Starting prediction evaluation cycle.");

                // 1. Get all MarketIds that have unevaluated predictions
                var marketIdsWithUnevaluatedPredictions = await dbContext.Predictions
                    .Where(p => p.EvaluatedAtUtc == null)
                    .Select(p => p.MarketId)
                    .Distinct()
                    .ToListAsync(stoppingToken);

                // 2. Find which of those markets actually have an outcome
                var marketsToEvaluate = await dbContext.Markets
                    .Where(m => marketIdsWithUnevaluatedPredictions.Contains(m.Id))
                    .Where(m => m.ActualOutcome != null && m.ResolvedAtUtc != null)
                    .ToListAsync(stoppingToken);

                int evaluatedCount = 0;

                foreach (var market in marketsToEvaluate)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    try
                    {
                        _logger.LogInformation("Unevaluated predictions detected for resolved MarketId={MarketId}", market.Id);
                        
                        await evaluationService.EvaluateMarketPredictionsAsync(market, market.ActualOutcome!, stoppingToken);
                        evaluatedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to evaluate predictions for market {MarketId}.", market.Id);
                    }
                }

                if (evaluatedCount > 0)
                {
                    var summary = await statisticsService.GetAccuracySummaryAsync(stoppingToken);
                    _logger.LogInformation("Prediction evaluation cycle completed. MarketsEvaluated: {Processed}, CurrentAccuracyPercentage: {Accuracy}%, AverageBrierScore: {BrierScore}",
                        evaluatedCount, summary.AccuracyPercentage, summary.AverageBrierScore);
                }
                else
                {
                    _logger.LogInformation("Prediction evaluation cycle completed. No pending evaluations.");
                }
                
                using var scopeH = _serviceProvider.CreateScope();
                var heartbeatServiceH = scopeH.ServiceProvider.GetRequiredService<IWorkerHeartbeatService>();
                await heartbeatServiceH.LogHeartbeatAsync(nameof(PredictionEvaluationWorker), "Healthy", null, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in PredictionEvaluationWorker.");
                try
                {
                    using var errScope = _serviceProvider.CreateScope();
                    var heartbeatService = errScope.ServiceProvider.GetRequiredService<IWorkerHeartbeatService>();
                    await heartbeatService.LogHeartbeatAsync(nameof(PredictionEvaluationWorker), "Error", ex.Message, stoppingToken);
                }
                catch { }
            }

            if (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}
