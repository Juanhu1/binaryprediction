using BinaryPrediction.Core.Common;
using Microsoft.EntityFrameworkCore;
using BinaryPrediction.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BinaryPrediction.Worker.Jobs;

public class MarketAnalysisProcessorWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MarketAnalysisProcessorWorker> _logger;

    public MarketAnalysisProcessorWorker(IServiceProvider serviceProvider, ILogger<MarketAnalysisProcessorWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    private int _analysesThisMinute = 0;
    private DateTimeOffset _currentMinute = DateTimeOffset.UtcNow;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        _logger.LogInformation("MarketAnalysisProcessorWorker starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var queueService = scope.ServiceProvider.GetRequiredService<IMarketAnalysisQueueService>();
                var aiAnalysisService = scope.ServiceProvider.GetRequiredService<IAiAnalysisService>();
                var settings = scope.ServiceProvider.GetRequiredService<IOptions<QueueProcessingSettings>>().Value;
                var openAiSettings = scope.ServiceProvider.GetRequiredService<IOptions<OpenAiSettings>>().Value;
                var dbContext = scope.ServiceProvider.GetRequiredService<BinaryPrediction.Infrastructure.Persistence.BinaryPredictionDbContext>();

                var pendingItems = await queueService.GetPendingItemsAsync(settings.BatchSize, stoppingToken);

                foreach (var item in pendingItems)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    var todayUtc = new DateTimeOffset(DateTimeOffset.UtcNow.UtcDateTime.Date, TimeSpan.Zero);
                    var analysesToday = await dbContext.Set<BinaryPrediction.Core.Entities.AiAnalysis>()
                        .CountAsync(a => a.CreatedAtUtc >= todayUtc, stoppingToken);
                    
                    if (analysesToday >= openAiSettings.DailyAnalysisLimit)
                    {
                        _logger.LogWarning("Daily AI budget reached. Queue processing paused.");
                        await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                        break;
                    }

                    if (DateTimeOffset.UtcNow.Minute != _currentMinute.Minute)
                    {
                        _currentMinute = DateTimeOffset.UtcNow;
                        _analysesThisMinute = 0;
                    }

                    if (_analysesThisMinute >= openAiSettings.MaxAnalysesPerMinute)
                    {
                        _logger.LogWarning("OpenAI rate limit reached. Deferring remaining queue items.");
                        await Task.Delay(TimeSpan.FromSeconds(60 - DateTimeOffset.UtcNow.Second), stoppingToken);
                        break;
                    }
                    if (stoppingToken.IsCancellationRequested) break;

                    _logger.LogInformation("Queue item picked: {QueueItemId}", item.Id);
                    await queueService.MarkProcessingAsync(item.Id, stoppingToken);

                    try 
                    {
                        // Ensure market is loaded
                        var market = await dbContext.Markets.FindAsync([item.MarketId], stoppingToken);
                        if (market != null)
                        {
                            // Process AI analysis using the queue
                            await aiAnalysisService.ProcessMarketAsync(market, stoppingToken);
                            _analysesThisMinute++;
                            await queueService.MarkCompletedAsync(item.Id, stoppingToken);
                        }
                        else
                        {
                            await queueService.MarkFailedAsync(item.Id, "Market not found", stoppingToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to analyze market {MarketId}", item.MarketId);
                        await queueService.MarkFailedAsync(item.Id, ex.Message, stoppingToken);
                    }
                }

                // If no items were processed, wait a bit before polling again
                if (!pendingItems.Any())
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing market analysis queue.");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
