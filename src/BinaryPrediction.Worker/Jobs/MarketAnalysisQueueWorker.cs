using BinaryPrediction.Core.Common;
using BinaryPrediction.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BinaryPrediction.Worker.Jobs;

public class MarketAnalysisQueueWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MarketAnalysisQueueWorker> _logger;

    public MarketAnalysisQueueWorker(IServiceProvider serviceProvider, ILogger<MarketAnalysisQueueWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        _logger.LogInformation("MarketAnalysisQueueWorker starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var queueService = scope.ServiceProvider.GetRequiredService<IMarketAnalysisQueueService>();
                var settings = scope.ServiceProvider.GetRequiredService<IOptions<WorkerSettings>>().Value;
                
                await queueService.RecoverStuckItemsAsync(stoppingToken);
                await queueService.EnqueueEligibleMarketsAsync(stoppingToken);
                await queueService.LogQueueStatusAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while enqueueing eligible markets.");
            }

            var scopeDelay = _serviceProvider.CreateScope();
            var delaySettings = scopeDelay.ServiceProvider.GetRequiredService<IOptions<WorkerSettings>>().Value;
            var interval = TimeSpan.FromMinutes(Math.Max(delaySettings.QueueCreationMinutes, 1));
            await Task.Delay(interval, stoppingToken);
        }
    }
}
