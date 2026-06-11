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
        await Task.Yield(); // Ensure we don't block Host startup
        _logger.LogInformation("PredictionWorker starting.");

        while (!cancellationToken.IsCancellationRequested)
        {
            using var workerScope = _logger.BeginScope(new Dictionary<string, object> { ["WorkerName"] = nameof(PredictionWorker) });
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<BinaryPredictionDbContext>();
                var predictionService = scope.ServiceProvider.GetRequiredService<IPredictionService>();
                var heartbeatService = scope.ServiceProvider.GetRequiredService<IWorkerHeartbeatService>();

                await heartbeatService.LogHeartbeatAsync(nameof(PredictionWorker), "Processing", null, cancellationToken);

                // Find top 10 analyses that don't have a prediction yet
                var analysesWithoutPredictions = await dbContext.AiAnalyses
                    .Where(a => !dbContext.Predictions.Any(p => p.AnalysisId == a.Id))
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
                        
                        using var itemScope = _logger.BeginScope(new Dictionary<string, object> 
                        { 
                            ["AnalysisId"] = analysis.Id, 
                            ["MarketId"] = analysis.MarketId 
                        });

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
                    _logger.LogDebug("PredictionWorker found 0 eligible analyses. Sleeping for 30s.");
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(30), CancellationToken.None);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("PredictionWorker cancellation requested – exiting loop gracefully.");
                        break;
                    }
                }

                await heartbeatService.LogHeartbeatAsync(nameof(PredictionWorker), "Healthy", null, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("PredictionWorker cancellation requested – exiting loop gracefully.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in PredictionWorker.");
                try
                {
                    using var errScope = _serviceProvider.CreateScope();
                    var heartbeatService = errScope.ServiceProvider.GetRequiredService<IWorkerHeartbeatService>();
                    await heartbeatService.LogHeartbeatAsync(nameof(PredictionWorker), "Error", ex.Message, cancellationToken);
                }
                catch { }
                await Task.Delay(TimeSpan.FromSeconds(30), CancellationToken.None);
            }
        }
    }
}
