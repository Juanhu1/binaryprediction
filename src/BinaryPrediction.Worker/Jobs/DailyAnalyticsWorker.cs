using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;
using BinaryPrediction.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BinaryPrediction.Worker.Jobs
{
    public class DailyAnalyticsWorker : BackgroundService
    {
        private readonly ILogger<DailyAnalyticsWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public DailyAnalyticsWorker(ILogger<DailyAnalyticsWorker> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DailyAnalyticsWorker started at: {time}", DateTimeOffset.UtcNow);
            // Run snapshots immediately on startup
            try
            {
                using var initScope = _scopeFactory.CreateScope();
                var initService = initScope.ServiceProvider.GetRequiredService<IPerformanceSnapshotService>();
                _logger.LogInformation("Generating daily analytics snapshots...");
                await initService.GenerateDailySnapshotAsync();
                await initService.GenerateCategorySnapshotsAsync();
                await initService.GenerateCalibrationSnapshotsAsync();
                _logger.LogInformation("Daily analytics snapshots generated at: {time}", DateTimeOffset.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating daily analytics snapshots");
            }
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var snapshotService = scope.ServiceProvider.GetRequiredService<IPerformanceSnapshotService>();
                    await snapshotService.GenerateDailySnapshotAsync();
                    await snapshotService.GenerateCategorySnapshotsAsync();
                    await snapshotService.GenerateCalibrationSnapshotsAsync();
                    _logger.LogInformation("Daily analytics snapshots generated at: {time}", DateTimeOffset.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating daily analytics snapshots");
                }

                // Wait 24 hours
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
            _logger.LogInformation("DailyAnalyticsWorker stopping at: {time}", DateTimeOffset.UtcNow);
        }
    }
}
