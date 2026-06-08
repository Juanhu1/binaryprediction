using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BinaryPrediction.Worker.Jobs;

public class PredictionQualityWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PredictionQualityWorker> _logger;

    public PredictionQualityWorker(IServiceProvider serviceProvider, ILogger<PredictionQualityWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("PredictionQualityWorker starting.");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<BinaryPredictionDbContext>();
                var qualityService = scope.ServiceProvider.GetRequiredService<IPredictionQualityService>();
                var performanceRepository = scope.ServiceProvider.GetRequiredService<IPredictionPerformanceRepository>();

                var today = DateTimeOffset.UtcNow.Date;

                // Check if a snapshot already exists for today
                var existingSnapshot = await dbContext.PredictionQualitySnapshots
                    .AnyAsync(s => s.SnapshotDateUtc == today, cancellationToken);

                if (!existingSnapshot)
                {
                    _logger.LogInformation("Generating daily prediction quality snapshot...");

                    var report = await qualityService.GenerateAsync(cancellationToken);
                    var totalPredictions = await performanceRepository.GetEvaluatedPredictionCountAsync(cancellationToken);

                    var snapshot = new PredictionQualitySnapshot
                    {
                        Id = Guid.NewGuid(),
                        SnapshotDateUtc = today,
                        TotalPredictions = totalPredictions,
                        AccuracyPercentage = report.Accuracy,
                        AverageBrierScore = report.AverageBrierScore,
                        CalibrationError = report.CalibrationError,
                        BenchmarkAdvantage = report.BenchmarkAdvantage,
                        ImprovementTrend = report.ImprovementTrend,
                        CreatedAtUtc = DateTimeOffset.UtcNow
                    };

                    dbContext.PredictionQualitySnapshots.Add(snapshot);
                    await dbContext.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Created quality snapshot. Calibration Error: {Calibration}, Benchmark Advantage: {Advantage}",
                        report.CalibrationError, report.BenchmarkAdvantage);
                }

                // Wait until tomorrow
                var tomorrow = today.AddDays(1);
                var delay = tomorrow - DateTimeOffset.UtcNow;
                if (delay.TotalMilliseconds > 0)
                {
                    _logger.LogInformation("PredictionQualityWorker sleeping for {Hours} hours until next snapshot.", Math.Round(delay.TotalHours, 1));
                    await Task.Delay(delay, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating prediction quality snapshot.");
                await Task.Delay(TimeSpan.FromHours(1), cancellationToken); // Retry in 1 hour if it fails
            }
        }
    }
}
