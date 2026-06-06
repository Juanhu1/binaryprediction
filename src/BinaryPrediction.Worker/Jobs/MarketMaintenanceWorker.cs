using BinaryPrediction.Core.Common;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BinaryPrediction.Worker.Jobs;

public class MarketMaintenanceWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MarketMaintenanceWorker> _logger;

    public MarketMaintenanceWorker(IServiceProvider serviceProvider, ILogger<MarketMaintenanceWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MarketMaintenanceWorker starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<BinaryPredictionDbContext>();
                var queueSettings = scope.ServiceProvider.GetRequiredService<IOptions<QueueProcessingSettings>>().Value;
                
                var cutoffDate = DateTimeOffset.UtcNow.AddDays(-queueSettings.ArchiveAfterDays);

                var itemsDeleted = await dbContext.MarketAnalysisQueueItems
                    .Where(q => q.CreatedAtUtc < cutoffDate)
                    .ExecuteDeleteAsync(stoppingToken);

                if (itemsDeleted > 0)
                {
                    _logger.LogInformation("MarketMaintenanceWorker archived/deleted {Count} old queue items.", itemsDeleted);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during queue maintenance.");
            }

            var scopeDelay = _serviceProvider.CreateScope();
            var delaySettings = scopeDelay.ServiceProvider.GetRequiredService<IOptions<WorkerSettings>>().Value;
            var interval = TimeSpan.FromMinutes(Math.Max(delaySettings.MaintenanceMinutes, 1));
            await Task.Delay(interval, stoppingToken);
        }
    }
}
