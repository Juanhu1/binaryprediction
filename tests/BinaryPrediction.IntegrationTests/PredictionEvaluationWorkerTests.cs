using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using BinaryPrediction.Infrastructure.Services;
using BinaryPrediction.Worker.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace BinaryPrediction.IntegrationTests;

public class PredictionEvaluationWorkerTests : IAsyncLifetime
{
    private BinaryPredictionDbContext _dbContext = null!;
    private ServiceProvider _serviceProvider = null!;
    private PredictionEvaluationWorker _worker = null!;

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();

        var dbName = Guid.NewGuid().ToString();
        services.AddDbContext<BinaryPredictionDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: dbName));

        services.AddScoped<IPredictionRepository, BinaryPrediction.Infrastructure.Persistence.Repositories.PredictionRepository>();
        services.AddScoped<IPredictionEvaluationService, PredictionEvaluationService>();
        services.AddScoped<IPredictionStatisticsService, PredictionStatisticsService>();
        
        // Mock loggers
        services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>));

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<BinaryPredictionDbContext>();
        
        await _dbContext.Database.EnsureCreatedAsync();

        _worker = new PredictionEvaluationWorker(_serviceProvider, NullLogger<PredictionEvaluationWorker>.Instance);
    }

    public Task DisposeAsync()
    {
        _dbContext.Dispose();
        _serviceProvider.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Worker_EvaluatesEligiblePredictions()
    {
        // Arrange
        var marketId = Guid.NewGuid();
        var market = new Market 
        { 
            Id = marketId, 
            Question = "Test Market", 
            Slug = "test",
            ActualOutcome = "Yes", // Manually resolved
            ResolvedAtUtc = DateTimeOffset.UtcNow,
            Closed = true 
        };
        
        var analysis = new AiAnalysis { Id = Guid.NewGuid(), MarketId = market.Id };
        
        var prediction = new Prediction
        {
            Id = Guid.NewGuid(),
            MarketId = market.Id,
            AnalysisId = analysis.Id,
            PredictedOutcome = "Yes",
            ConfidencePercentage = 95m,
            IsActive = true,
            EvaluatedAtUtc = null // Unevaluated
        };

        _dbContext.Markets.Add(market);
        _dbContext.AiAnalyses.Add(analysis);
        _dbContext.Predictions.Add(prediction);
        await _dbContext.SaveChangesAsync();

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try
        {
            var method = _worker.GetType().GetMethod("ExecuteAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                await (Task)method.Invoke(_worker, new object[] { cts.Token })!;
            }
        }
        catch (Exception ex) when (ex.InnerException is OperationCanceledException || ex is OperationCanceledException)
        {
            // Expected due to cancellation
        }
        catch (Exception ex)
        {
            throw new Exception("ExecuteAsync threw an exception: " + ex.ToString());
        }

        var evaluated = await _dbContext.Predictions.AsNoTracking().FirstOrDefaultAsync(p => p.Id == prediction.Id);

        // Assert
        Assert.NotNull(evaluated);
        Assert.NotNull(evaluated.EvaluatedAtUtc);
        Assert.True(evaluated.WasCorrect);
        Assert.NotNull(evaluated.BrierScore);
    }
}
