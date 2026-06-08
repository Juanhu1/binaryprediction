using BinaryPrediction.Core.Entities;
using BinaryPrediction.Infrastructure.Persistence;
using BinaryPrediction.Infrastructure.Persistence.Repositories;
using BinaryPrediction.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BinaryPrediction.IntegrationTests;

public class PredictionBenchmarkIntegrationTests : IAsyncLifetime
{
    private BinaryPredictionDbContext _dbContext = null!;
    private PredictionBenchmarkService _benchmarkService = null!;

    public async Task InitializeAsync()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<BinaryPredictionDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        _dbContext = new BinaryPredictionDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        var repo = new PredictionPerformanceRepository(_dbContext);
        _benchmarkService = new PredictionBenchmarkService(repo, _dbContext, NullLogger<PredictionBenchmarkService>.Instance);
    }

    public Task DisposeAsync()
    {
        _dbContext.Dispose();
        return Task.CompletedTask;
    }

    private async Task SeedDataAsync()
    {
        var market1 = new Market { Id = Guid.NewGuid(), Question = "M1", ActualOutcome = "Yes", ResolvedAtUtc = DateTimeOffset.UtcNow, Closed = true, Slug="1" };
        var market2 = new Market { Id = Guid.NewGuid(), Question = "M2", ActualOutcome = "No", ResolvedAtUtc = DateTimeOffset.UtcNow, Closed = true, Slug="2" };
        var market3 = new Market { Id = Guid.NewGuid(), Question = "M3", ActualOutcome = "Yes", ResolvedAtUtc = DateTimeOffset.UtcNow, Closed = true, Slug="3" };

        var analysis1 = new AiAnalysis { Id = Guid.NewGuid(), MarketId = market1.Id };
        var analysis2 = new AiAnalysis { Id = Guid.NewGuid(), MarketId = market2.Id };
        var analysis3 = new AiAnalysis { Id = Guid.NewGuid(), MarketId = market3.Id };

        _dbContext.Markets.AddRange(market1, market2, market3);
        _dbContext.AiAnalyses.AddRange(analysis1, analysis2, analysis3);

        var p1 = new Prediction { Id = Guid.NewGuid(), MarketId = market1.Id, PredictedOutcome = "Yes", ActualOutcome = "Yes", WasCorrect = true, EvaluatedAtUtc = DateTimeOffset.UtcNow, ConfidencePercentage = 80, BrierScore = 0.04m, IsActive = true, Market = market1 };
        var p2 = new Prediction { Id = Guid.NewGuid(), MarketId = market2.Id, PredictedOutcome = "Yes", ActualOutcome = "No", WasCorrect = false, EvaluatedAtUtc = DateTimeOffset.UtcNow, ConfidencePercentage = 60, BrierScore = 0.36m, IsActive = true, Market = market2 };
        var p3 = new Prediction { Id = Guid.NewGuid(), MarketId = market3.Id, PredictedOutcome = "No", ActualOutcome = "Yes", WasCorrect = false, EvaluatedAtUtc = DateTimeOffset.UtcNow, ConfidencePercentage = 90, BrierScore = 0.81m, IsActive = true, Market = market3 };

        _dbContext.Predictions.AddRange(p1, p2, p3);
        await _dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task EvaluateAlwaysYes_CalculatesCorrectly()
    {
        await SeedDataAsync();

        var result = await _benchmarkService.EvaluateAlwaysYesAsync();

        Assert.Equal("Always Yes", result.BenchmarkType);
        Assert.Equal(3, result.TotalPredictions);
        Assert.Equal(2, result.CorrectPredictions); // M1 (Yes), M3 (Yes)
        Assert.Equal(66.67m, Math.Round(result.AccuracyPercentage, 2));
        
        // Brier: M1: (1-1)^2 = 0, M2: (1-0)^2 = 1, M3: (1-1)^2 = 0 => sum=1, avg=0.3333
        Assert.Equal(0.3333m, Math.Round(result.AverageBrierScore, 4));
    }

    [Fact]
    public async Task EvaluateAlwaysNo_CalculatesCorrectly()
    {
        await SeedDataAsync();

        var result = await _benchmarkService.EvaluateAlwaysNoAsync();

        Assert.Equal("Always No", result.BenchmarkType);
        Assert.Equal(3, result.TotalPredictions);
        Assert.Equal(1, result.CorrectPredictions); // M2 (No)
        Assert.Equal(33.33m, Math.Round(result.AccuracyPercentage, 2));
        
        // Brier: M1: (0-1)^2 = 1, M2: (0-0)^2 = 0, M3: (0-1)^2 = 1 => sum=2, avg=0.6667
        Assert.Equal(0.6667m, Math.Round(result.AverageBrierScore, 4));
    }

    [Fact]
    public async Task EvaluateAi_CalculatesCorrectly()
    {
        await SeedDataAsync();

        var result = await _benchmarkService.EvaluateAiAsync();

        Assert.Equal("AI", result.BenchmarkType);
        Assert.Equal(3, result.TotalPredictions);
        Assert.Equal(1, result.CorrectPredictions); // p1 is correct
        Assert.Equal(33.33m, Math.Round(result.AccuracyPercentage, 2));
        
        // Brier: (0.04 + 0.36 + 0.81) / 3 = 0.4033
        Assert.Equal(0.4033m, Math.Round(result.AverageBrierScore, 4));
    }

    [Fact]
    public async Task EvaluateRandom_IsDeterministic()
    {
        await SeedDataAsync();

        var result1 = await _benchmarkService.EvaluateRandomAsync();
        var result2 = await _benchmarkService.EvaluateRandomAsync();

        Assert.Equal("Random", result1.BenchmarkType);
        Assert.Equal(result1.TotalPredictions, result2.TotalPredictions);
        Assert.Equal(result1.CorrectPredictions, result2.CorrectPredictions);
        Assert.Equal(result1.AccuracyPercentage, result2.AccuracyPercentage);
        Assert.Equal(result1.AverageBrierScore, result2.AverageBrierScore);
    }
}
