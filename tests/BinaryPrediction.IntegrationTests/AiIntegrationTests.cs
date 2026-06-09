using BinaryPrediction.Core.Entities;
using BinaryPrediction.Infrastructure.Persistence;
using BinaryPrediction.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace BinaryPrediction.IntegrationTests;

public class AiIntegrationTests : IAsyncLifetime
{
    private BinaryPredictionDbContext _dbContext = null!;
    private AiPerformanceService _performanceService = null!;

    public async Task InitializeAsync()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<BinaryPredictionDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        _dbContext = new BinaryPredictionDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        _performanceService = new AiPerformanceService(_dbContext);
    }

    public Task DisposeAsync()
    {
        _dbContext.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Can_Query_Performance_Endpoint()
    {
        _dbContext.AiUsageRecords.Add(new AiUsageRecord
        {
            Id = Guid.NewGuid(),
            MarketId = Guid.NewGuid(),
            OperationType = "Analysis",
            Model = "test-model",
            PromptTokens = 100,
            CompletionTokens = 50,
            TotalTokens = 150,
            EstimatedCostUsd = 0.0025m,
            LatencyMs = 1200,
            IsSuccess = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        _dbContext.AiUsageRecords.Add(new AiUsageRecord
        {
            Id = Guid.NewGuid(),
            MarketId = Guid.NewGuid(),
            OperationType = "Prediction",
            Model = "test-model",
            PromptTokens = 150,
            CompletionTokens = 20,
            TotalTokens = 170,
            EstimatedCostUsd = 0.0021m,
            LatencyMs = 800,
            IsSuccess = false,
            ErrorMessage = "Test error",
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync();

        var result = await _performanceService.GetPerformanceAsync();

        Assert.Equal(1, result.AnalysesGenerated);
        Assert.Equal(0, result.FailedAnalyses);
        Assert.Equal(0, result.PredictionsGenerated);
        Assert.Equal(1, result.FailedPredictions);
        Assert.Equal(1000, result.AverageLatencyMs);
        Assert.Equal(0.0023m, result.AverageCostUsd);
    }
}
