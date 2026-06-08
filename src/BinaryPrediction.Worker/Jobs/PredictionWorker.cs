using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BinaryPrediction.Worker.Jobs;

public class PredictionWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PredictionWorker> _logger;
    private readonly HashSet<Guid> _failedAnalyses = new();

    public PredictionWorker(IServiceProvider serviceProvider, ILogger<PredictionWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("PredictionWorker starting.");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<BinaryPredictionDbContext>();
                var predictionService = scope.ServiceProvider.GetRequiredService<IPredictionService>();

                // Find top 10 analyses for markets that don't have a prediction yet
                var analysesWithoutPredictions = await dbContext.AiAnalyses
                    .Where(a => !dbContext.Predictions.Any(p => p.MarketId == a.MarketId))
                    .OrderBy(a => a.CreatedAtUtc)
                    .Take(50)
                    .ToListAsync(cancellationToken);

                var toProcess = analysesWithoutPredictions.Where(a => !_failedAnalyses.Contains(a.Id)).Take(10).ToList();

                if (toProcess.Any())
                {
                    _logger.LogInformation("Found {Count} analyses without predictions. Starting prediction generation.", toProcess.Count);

                    foreach (var analysis in toProcess)
                    {
                        if (cancellationToken.IsCancellationRequested) break;

                        var market = await dbContext.Markets.FindAsync(new object[] { analysis.MarketId }, cancellationToken);
                        if (market == null)
                        {
                            _logger.LogWarning("Market {MarketId} not found for analysis {AnalysisId}. Skipping prediction.", analysis.MarketId, analysis.Id);
                            _failedAnalyses.Add(analysis.Id);
                            continue;
                        }

                        try
                        {
                            var prediction = await predictionService.CreatePredictionAsync(analysis, market, cancellationToken);
                            if (prediction == null)
                            {
                                // CreatePredictionAsync skipped it or failed confidence check.
                                _failedAnalyses.Add(analysis.Id);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to generate prediction for market {MarketId} (Analysis: {AnalysisId})", market.Id, analysis.Id);
                            _failedAnalyses.Add(analysis.Id);
                        }
                    }
                }
                else
                {
                    // If no items were processed, wait a bit before polling again
                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in PredictionWorker.");
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }
    }
}
