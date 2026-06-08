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

    public PredictionWorker(IServiceProvider serviceProvider, ILogger<PredictionWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PredictionWorker starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<BinaryPredictionDbContext>();
                var predictionService = scope.ServiceProvider.GetRequiredService<IPredictionService>();
                var predictionRepo = scope.ServiceProvider.GetRequiredService<IPredictionRepository>();

                // Find top 10 analyses that don't have a prediction yet
                // To do this, we need to query analyses and check if their ID doesn't exist in Predictions table
                var analysesWithoutPredictions = await dbContext.AiAnalyses
                    .Where(a => !dbContext.Predictions.Any(p => p.AnalysisId == a.Id))
                    .OrderBy(a => a.CreatedAtUtc)
                    .Take(10)
                    .ToListAsync(stoppingToken);

                if (analysesWithoutPredictions.Any())
                {
                    _logger.LogInformation("Found {Count} analyses without predictions. Starting prediction generation.", analysesWithoutPredictions.Count);

                    foreach (var analysis in analysesWithoutPredictions)
                    {
                        if (stoppingToken.IsCancellationRequested) break;

                        var market = await dbContext.Markets.FindAsync(new object[] { analysis.MarketId }, stoppingToken);
                        if (market == null)
                        {
                            _logger.LogWarning("Market {MarketId} not found for analysis {AnalysisId}. Skipping prediction.", analysis.MarketId, analysis.Id);
                            continue;
                        }

                        try
                        {
                            await predictionService.CreatePredictionAsync(analysis, market, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to generate prediction for market {MarketId} (Analysis: {AnalysisId})", market.Id, analysis.Id);
                        }
                    }
                }
                else
                {
                    // If no items were processed, wait a bit before polling again
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in PredictionWorker.");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
