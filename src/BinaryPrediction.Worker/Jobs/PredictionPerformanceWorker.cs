using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using BinaryPrediction.Core.Interfaces;

namespace BinaryPrediction.Worker.Jobs;

/// <summary>
/// Background worker that generates a daily <see cref="PredictionPerformanceSnapshot"/> using
/// <see cref="IPredictionPerformanceService"/>. The service itself guards against
/// duplicate snapshots for the same UTC date.
/// </summary>
public class PredictionPerformanceWorker : BackgroundService
{
    private readonly ILogger<PredictionPerformanceWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public PredictionPerformanceWorker(ILogger<PredictionPerformanceWorker> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PredictionPerformanceWorker started at: {time}", DateTimeOffset.UtcNow);

        // Run once immediately on start so the first snapshot is available.
        await RunSnapshotAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            // Wait 24 hours before generating the next snapshot.
            try
            {
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
            catch (TaskCanceledException) { /* shutdown */ }

            if (!stoppingToken.IsCancellationRequested)
                await RunSnapshotAsync(stoppingToken);
        }

        _logger.LogInformation("PredictionPerformanceWorker stopping at: {time}", DateTimeOffset.UtcNow);
    }

    private async Task RunSnapshotAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IPredictionPerformanceService>();
            await service.GenerateDailySnapshotAsync();
            _logger.LogInformation("Daily prediction performance snapshot generated at: {time}", DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating daily prediction performance snapshot");
        }
    }
}
