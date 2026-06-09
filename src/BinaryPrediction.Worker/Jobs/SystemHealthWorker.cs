using BinaryPrediction.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BinaryPrediction.Worker.Jobs;

public class SystemHealthWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SystemHealthWorker> _logger;

    public SystemHealthWorker(IServiceProvider serviceProvider, ILogger<SystemHealthWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();
        _logger.LogInformation("SystemHealthWorker starting.");

        while (!cancellationToken.IsCancellationRequested)
        {
            using var workerScope = _logger.BeginScope(new Dictionary<string, object> { ["WorkerName"] = nameof(SystemHealthWorker) });
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var healthService = scope.ServiceProvider.GetRequiredService<ISystemHealthService>();
                var heartbeatService = scope.ServiceProvider.GetRequiredService<IWorkerHeartbeatService>();

                await heartbeatService.LogHeartbeatAsync(nameof(SystemHealthWorker), "Processing", null, cancellationToken);
                
                await healthService.CreateSnapshotAsync(cancellationToken);
                
                await heartbeatService.LogHeartbeatAsync(nameof(SystemHealthWorker), "Healthy", null, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating system health snapshot.");
                try
                {
                    using var errScope = _serviceProvider.CreateScope();
                    var heartbeatService = errScope.ServiceProvider.GetRequiredService<IWorkerHeartbeatService>();
                    await heartbeatService.LogHeartbeatAsync(nameof(SystemHealthWorker), "Error", ex.Message, cancellationToken);
                }
                catch { }
            }

            // Run every 1 hour
            await Task.Delay(TimeSpan.FromHours(1), cancellationToken);
        }
    }
}
