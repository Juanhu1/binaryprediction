using BinaryPrediction.Core.Common;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Core.Services;
using BinaryPrediction.Infrastructure.External.Polymarket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BinaryPrediction.Worker.Jobs;

public class MarketCollectorWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<MarketCollectorWorker> _logger;
    private readonly WorkerSettings _settings;

    public MarketCollectorWorker(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<WorkerSettings> settings,
        ILogger<MarketCollectorWorker> logger)
    {
        Console.WriteLine("MARKET WORKER CONSTRUCTOR");
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield(); // Ensure we don't block Host startup
        var interval = TimeSpan.FromMinutes(Math.Max(_settings.MarketCollectionMinutes, 1));
        _logger.LogInformation("Market collector worker started with interval {Interval}.", interval);

        using var timer = new PeriodicTimer(interval);

        await RunCollectionAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunCollectionAsync(stoppingToken);
        }
    }

    private async Task LogHeartbeatAsync(string status, string? errorMessage = null)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var heartbeatService = scope.ServiceProvider.GetRequiredService<IWorkerHeartbeatService>();
        await heartbeatService.LogHeartbeatAsync(nameof(MarketCollectorWorker), status, errorMessage);
    }

    private async Task RunCollectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            await LogHeartbeatAsync("Processing");
            using var scope = _serviceScopeFactory.CreateScope();
            var synchronizationService = scope.ServiceProvider.GetRequiredService<IMarketSynchronizationService>();

            _logger.LogInformation("Starting Polymarket market collection.");
            await synchronizationService.SynchronizeActiveMarketsAsync(cancellationToken);
            _logger.LogInformation("Finished Polymarket market collection.");
            await LogHeartbeatAsync("Healthy");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Market collector worker is stopping.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Market collection failed.");
            await LogHeartbeatAsync("Error", ex.Message);
        }
    }
}
