using BinaryPrediction.Core.Entities;
using BinaryPrediction.Infrastructure.Persistence;
using BinaryPrediction.Infrastructure.Persistence.Repositories;
using BinaryPrediction.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BinaryPrediction.IntegrationTests;

public class PredictionDashboardIntegrationTests : IAsyncLifetime
{
    private BinaryPredictionDbContext _dbContext = null!;
    private PredictionDashboardService _dashboardService = null!;

    public async Task InitializeAsync()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<BinaryPredictionDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        _dbContext = new BinaryPredictionDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        var perfRepo = new PredictionPerformanceRepository(_dbContext);
        var statService = new PredictionStatisticsService(_dbContext);
        var benchmarkService = new PredictionBenchmarkService(perfRepo, _dbContext, NullLogger<PredictionBenchmarkService>.Instance);
        
        _dashboardService = new PredictionDashboardService(perfRepo, benchmarkService, statService, NullLogger<PredictionDashboardService>.Instance);
    }

    public Task DisposeAsync()
    {
        _dbContext.Dispose();
        return Task.CompletedTask;
    }

    private async Task SeedDataAsync()
    {
        var m1 = new Market { Id = Guid.NewGuid(), Question = "M1", ActualOutcome = "Yes", ResolvedAtUtc = DateTimeOffset.UtcNow, Closed = true, Slug="1" };
        var m2 = new Market { Id = Guid.NewGuid(), Question = "M2", ActualOutcome = "No", ResolvedAtUtc = DateTimeOffset.UtcNow, Closed = true, Slug="2" };
        
        _dbContext.Markets.AddRange(m1, m2);

        var a1 = new AiAnalysis { Id = Guid.NewGuid(), MarketId = m1.Id };
        var a2 = new AiAnalysis { Id = Guid.NewGuid(), MarketId = m2.Id };
        
        _dbContext.AiAnalyses.AddRange(a1, a2);

        var date1 = DateTimeOffset.UtcNow.AddDays(-2);
        var date2 = DateTimeOffset.UtcNow.AddDays(-1);

        var p1 = new Prediction { Id = Guid.NewGuid(), MarketId = m1.Id, PredictedOutcome = "Yes", ActualOutcome = "Yes", WasCorrect = true, EvaluatedAtUtc = date1, ConfidencePercentage = 95, BrierScore = 0.0025m, IsActive = true, Market = m1 };
        var p2 = new Prediction { Id = Guid.NewGuid(), MarketId = m2.Id, PredictedOutcome = "Yes", ActualOutcome = "No", WasCorrect = false, EvaluatedAtUtc = date2, ConfidencePercentage = 60, BrierScore = 0.36m, IsActive = true, Market = m2 };

        _dbContext.Predictions.AddRange(p1, p2);
        await _dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetSummary_ReturnsCorrectMetrics()
    {
        await SeedDataAsync();

        var summary = await _dashboardService.GetSummaryAsync();

        Assert.Equal(2, summary.TotalMarkets);
        Assert.Equal(2, summary.ResolvedMarkets);
        Assert.Equal(2, summary.TotalPredictions);
        Assert.Equal(2, summary.EvaluatedPredictions);
        Assert.Equal(50.0m, summary.AccuracyPercentage);
        Assert.Equal(77.5m, summary.AverageConfidencePercentage);
        Assert.Equal(0.18125m, summary.AverageBrierScore);
        
        Assert.NotNull(summary.BestConfidenceBand);
        Assert.Equal("90-100", summary.BestConfidenceBand!.BandName);
        Assert.Equal(100.0m, summary.BestConfidenceBand.AccuracyPercentage);

        Assert.NotNull(summary.WorstConfidenceBand);
        Assert.Equal("60-69", summary.WorstConfidenceBand!.BandName);
        Assert.Equal(0.0m, summary.WorstConfidenceBand.AccuracyPercentage);
    }

    [Fact]
    public async Task GetDailyTrends_ReturnsTrends()
    {
        await SeedDataAsync();

        var trends = await _dashboardService.GetDailyAccuracyTrendAsync();

        Assert.Equal(2, trends.Count);
        
        var t1 = trends[0];
        Assert.Equal(1, t1.PredictionCount);
        Assert.Equal(100.0m, t1.AccuracyPercentage);
        
        var t2 = trends[1];
        Assert.Equal(1, t2.PredictionCount);
        Assert.Equal(0.0m, t2.AccuracyPercentage);
    }
}
