using BinaryPrediction.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BinaryPrediction.Worker.Jobs;

public class PredictionResolutionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PredictionResolutionWorker> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(30);

    public PredictionResolutionWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<PredictionResolutionWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initial delay to align schedule if needed
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Prediction resolution started at {Time}.", DateTimeOffset.UtcNow);

            using var scope = _scopeFactory.CreateScope();
            var resolutionService = scope.ServiceProvider.GetRequiredService<IPredictionResolutionService>();
            var resolutionRepository = scope.ServiceProvider.GetRequiredService<IPredictionResolutionRepository>();

            int processed = await resolutionService.ProcessPendingPredictionsAsync(stoppingToken);
            int pending = await resolutionRepository.GetPendingCountAsync(stoppingToken);

            _logger.LogInformation("Resolved predictions: {Processed}. Pending predictions remaining: {Pending}.", processed, pending);

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
}
