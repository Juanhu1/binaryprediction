using BinaryPrediction.Core.Common;
using BinaryPrediction.Core.Services;
using BinaryPrediction.Infrastructure.External.Polymarket;
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
        Console.WriteLine("MARKET WORKER EXECUTE");
        var interval = TimeSpan.FromMinutes(Math.Max(_settings.MarketCollectionMinutes, 1));
        _logger.LogInformation("Market collector worker started with interval {Interval}.", interval);

        using var timer = new PeriodicTimer(interval);

        await RunCollectionAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunCollectionAsync(stoppingToken);
        }
    }

    private async Task RunCollectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var synchronizationService = scope.ServiceProvider.GetRequiredService<IMarketSynchronizationService>();

            _logger.LogInformation("Starting Polymarket market collection.");
            await synchronizationService.SynchronizeActiveMarketsAsync(cancellationToken);
            _logger.LogInformation("Finished Polymarket market collection.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Market collector worker is stopping.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Market collection failed.");
        }
    }
}
