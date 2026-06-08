using BinaryPrediction.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BinaryPrediction.Worker.Jobs;

public class PredictionResolutionWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PredictionResolutionWorker> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(15); // Could be moved to settings

    public PredictionResolutionWorker(
        IServiceProvider serviceProvider,
        ILogger<PredictionResolutionWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PredictionResolutionWorker starting with interval {Interval}.", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var resolutionService = scope.ServiceProvider.GetRequiredService<IMarketResolutionService>();
                var statisticsService = scope.ServiceProvider.GetRequiredService<IPredictionStatisticsService>();

                var dbContext = scope.ServiceProvider.GetRequiredService<BinaryPrediction.Infrastructure.Persistence.BinaryPredictionDbContext>();

                _logger.LogInformation("Starting prediction resolution cycle.");

                var resolvedMarkets = await resolutionService.GetResolvedMarketsAsync(stoppingToken);

                int evaluatedCount = 0;

                foreach (var market in resolvedMarkets)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    try
                    {
                        _logger.LogInformation("Resolved market detected: MarketId={MarketId}, Question={Question}", market.Id, market.Question);

                        var outcome = await resolutionService.GetActualOutcomeAsync(market, stoppingToken);

                        if (outcome != null)
                        {
                            _logger.LogInformation("Actual outcome detected: MarketId={MarketId}, Outcome={Outcome}", market.Id, outcome);

                            // Persist actual outcome directly to Market
                            market.ActualOutcome = outcome;
                            market.ResolvedAtUtc = DateTimeOffset.UtcNow;
                            market.Closed = true;
                            await dbContext.SaveChangesAsync(stoppingToken);

                            // Evaluation is now handled by PredictionEvaluationWorker
                            evaluatedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Prediction resolution failed for market {MarketId}.", market.Id);
                    }
                }

                if (evaluatedCount > 0)
                {
                    var summary = await statisticsService.GetAccuracySummaryAsync(stoppingToken);
                    _logger.LogInformation("Prediction resolution completed. ResolvedMarketsProcessed: {Processed}, CurrentAccuracyPercentage: {Accuracy}%, AverageBrierScore: {BrierScore}",
                        evaluatedCount, summary.AccuracyPercentage, summary.AverageBrierScore);
                }
                else
                {
                    _logger.LogInformation("Prediction resolution completed. No new markets resolved.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in PredictionResolutionWorker.");
            }

            if (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}
