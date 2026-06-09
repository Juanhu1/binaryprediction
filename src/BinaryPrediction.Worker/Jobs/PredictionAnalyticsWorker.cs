using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BinaryPrediction.Infrastructure.Persistence;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace BinaryPrediction.Worker.Jobs
{
    public class PredictionAnalyticsWorker : BackgroundService
    {
        private readonly ILogger<PredictionAnalyticsWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeSpan _interval = TimeSpan.FromHours(1);
        private static readonly (decimal Min, decimal Max, string Label)[] _buckets = new[]
        {
            (50m, 60m, "50-59"),
            (60m, 70m, "60-69"),
            (70m, 80m, "70-79"),
            (80m, 90m, "80-89"),
            (90m, 100m, "90-100")
        };

        public PredictionAnalyticsWorker(ILogger<PredictionAnalyticsWorker> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PredictionAnalyticsWorker started, running every {Interval}.", _interval);
            using var initScope = _scopeFactory.CreateScope();
            var heartbeatService = initScope.ServiceProvider.GetRequiredService<IWorkerHeartbeatService>();
            await heartbeatService.LogHeartbeatAsync(nameof(PredictionAnalyticsWorker), "Running");
            try
            {
                // Initial run
                await RunAnalyticsAsync(stoppingToken);
                await heartbeatService.LogHeartbeatAsync(nameof(PredictionAnalyticsWorker), "Healthy");

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(_interval, stoppingToken);
                    }
                    catch (OperationCanceledException) { break; }

                    using var loopScope = _scopeFactory.CreateScope();
                    var loopHb = loopScope.ServiceProvider.GetRequiredService<IWorkerHeartbeatService>();
                    await RunAnalyticsAsync(stoppingToken);
                    await loopHb.LogHeartbeatAsync(nameof(PredictionAnalyticsWorker), "Healthy");
                }
            }
            catch (Exception ex)
            {
                using var errScope = _scopeFactory.CreateScope();
                var errHb = errScope.ServiceProvider.GetRequiredService<IWorkerHeartbeatService>();
                await errHb.LogHeartbeatAsync(nameof(PredictionAnalyticsWorker), "Error", ex.Message);
                _logger.LogError(ex, "PredictionAnalyticsWorker encountered an error.");
                throw;
            }
            finally
            {
                _logger.LogInformation("PredictionAnalyticsWorker stopping.");
                using var finalScope = _scopeFactory.CreateScope();
                var finalHb = finalScope.ServiceProvider.GetRequiredService<IWorkerHeartbeatService>();
                await finalHb.LogHeartbeatAsync(nameof(PredictionAnalyticsWorker), "Stopped");
            }
        }

        private async Task RunAnalyticsAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BinaryPredictionDbContext>();

            var utcNow = DateTimeOffset.UtcNow;
            var snapshots = new List<PredictionCalibrationSnapshot>();

            // Only consider finalized predictions (WasCorrect has a value)
            var predictions = dbContext.Predictions
                .Where(p => p.WasCorrect.HasValue)
                .Select(p => new { p.ConfidencePercentage, p.WasCorrect })
                .ToList();

            foreach (var bucket in _buckets)
            {
                var bucketPredictions = predictions
                    .Where(p => p.ConfidencePercentage >= bucket.Min && p.ConfidencePercentage < bucket.Max)
                    .ToList();

                if (bucketPredictions.Count == 0) continue;

                var actualAccuracy = bucketPredictions.Count(p => p.WasCorrect == true) / (double)bucketPredictions.Count;
                var expectedAccuracy = bucketPredictions.Average(p => (double)p.ConfidencePercentage) / 100.0;
                var error = Math.Abs(actualAccuracy - expectedAccuracy);

                var snapshot = new PredictionCalibrationSnapshot
                {
                    Id = Guid.NewGuid(),
                    CreatedAtUtc = utcNow,
                    ConfidenceRange = bucket.Label,
                    PredictionCount = bucketPredictions.Count,
                    ActualAccuracyPercentage = (decimal)(actualAccuracy * 100),
                    ExpectedAccuracyPercentage = (decimal)(expectedAccuracy * 100),
                    CalibrationError = (decimal)error,
                    Confidence = (bucket.Min + bucket.Max) / 2
                };
                snapshots.Add(snapshot);
            }

            if (snapshots.Any())
            {
                await dbContext.PredictionCalibrationSnapshots.AddRangeAsync(snapshots, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Inserted {Count} calibration snapshots.", snapshots.Count);
            }
            else
            {
                _logger.LogInformation("No predictions found for calibration at this run.");
            }
        }
    }
}
