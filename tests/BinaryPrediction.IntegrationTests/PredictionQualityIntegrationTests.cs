using BinaryPrediction.Core.Entities;
using BinaryPrediction.Infrastructure.Persistence;
using BinaryPrediction.Infrastructure.Persistence.Repositories;
using BinaryPrediction.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BinaryPrediction.IntegrationTests;

public class PredictionQualityIntegrationTests : IAsyncLifetime
{
    private BinaryPredictionDbContext _dbContext = null!;
    private PredictionQualityService _qualityService = null!;
    private ConfidenceBandService _confidenceBandService = null!;
    private MarketCategoryPerformanceService _categoryPerformanceService = null!;
    private PredictionsImprovementService _improvementService = null!;

    public async Task InitializeAsync()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<BinaryPredictionDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        _dbContext = new BinaryPredictionDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        var perfRepo = new PredictionPerformanceRepository(_dbContext);
        var predRepo = new BinaryPrediction.Infrastructure.Persistence.Repositories.PredictionRepository(_dbContext);
        
        var benchmarkService = new PredictionBenchmarkService(perfRepo, _dbContext, NullLogger<PredictionBenchmarkService>.Instance);
        var statsService = new PredictionStatisticsService(_dbContext);
        
        _qualityService = new PredictionQualityService(perfRepo, benchmarkService);
        _confidenceBandService = new ConfidenceBandService(statsService);
        _categoryPerformanceService = new MarketCategoryPerformanceService(_dbContext);
        
        _improvementService = new PredictionsImprovementService(
            _confidenceBandService,
            _categoryPerformanceService,
            _qualityService);
    }

    public Task DisposeAsync()
    {
        _dbContext.Dispose();
        return Task.CompletedTask;
    }

    private async Task SeedDataAsync()
    {
        // Category 1: Sports (Best)
        for (int i = 0; i < 6; i++)
        {
            var m = new Market { Id = Guid.NewGuid(), Category = BinaryPrediction.Core.Enums.MarketCategory.Sports, ActualOutcome = "Yes", ResolvedAtUtc = DateTimeOffset.UtcNow, Closed = true, Slug=$"sports-{i}" };
            var a = new AiAnalysis { Id = Guid.NewGuid(), MarketId = m.Id };
            var p = new Prediction { Id = Guid.NewGuid(), MarketId = m.Id, AnalysisId = a.Id, PredictedOutcome = "Yes", ActualOutcome = "Yes", WasCorrect = true, EvaluatedAtUtc = DateTimeOffset.UtcNow, ConfidencePercentage = 95, BrierScore = 0.01m, Market = m };
            _dbContext.Markets.Add(m);
            _dbContext.AiAnalyses.Add(a);
            _dbContext.Predictions.Add(p);
        }

        // Category 2: Crypto (Worst)
        for (int i = 0; i < 6; i++)
        {
            var m = new Market { Id = Guid.NewGuid(), Category = BinaryPrediction.Core.Enums.MarketCategory.Crypto, ActualOutcome = "Yes", ResolvedAtUtc = DateTimeOffset.UtcNow, Closed = true, Slug=$"crypto-{i}" };
            var a = new AiAnalysis { Id = Guid.NewGuid(), MarketId = m.Id };
            var p = new Prediction { Id = Guid.NewGuid(), MarketId = m.Id, AnalysisId = a.Id, PredictedOutcome = "No", ActualOutcome = "Yes", WasCorrect = false, EvaluatedAtUtc = DateTimeOffset.UtcNow, ConfidencePercentage = 55, BrierScore = 0.81m, Market = m };
            _dbContext.Markets.Add(m);
            _dbContext.AiAnalyses.Add(a);
            _dbContext.Predictions.Add(p);
        }

        // Add 1 historical prediction for trend comparison (accurate)
        var mOld = new Market { Id = Guid.NewGuid(), Category = BinaryPrediction.Core.Enums.MarketCategory.Sports, ActualOutcome = "Yes", ResolvedAtUtc = DateTimeOffset.UtcNow.AddDays(-40), Closed = true, Slug="old-sports" };
        var aOld = new AiAnalysis { Id = Guid.NewGuid(), MarketId = mOld.Id };
        var pOld = new Prediction { Id = Guid.NewGuid(), MarketId = mOld.Id, AnalysisId = aOld.Id, PredictedOutcome = "Yes", ActualOutcome = "Yes", WasCorrect = true, EvaluatedAtUtc = DateTimeOffset.UtcNow.AddDays(-40), ConfidencePercentage = 80, BrierScore = 0.04m, Market = mOld };
        
        _dbContext.Markets.Add(mOld);
        _dbContext.AiAnalyses.Add(aOld);
        _dbContext.Predictions.Add(pOld);

        await _dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task CalculateCalibrationError_WorksCorrectly()
    {
        await SeedDataAsync();
        
        // Total 13 predictions.
        // 6 Sports: Confidence 0.95, Correct (1.0). Error = 0.05
        // 6 Crypto: Confidence 0.55, Incorrect (0.0). Error = 0.55
        // 1 Old: Confidence 0.80, Correct (1.0). Error = 0.20
        // Sum of Errors: (6 * 0.05) + (6 * 0.55) + 0.20 = 0.30 + 3.30 + 0.20 = 3.80
        // Avg Error: 3.80 / 13 = 0.2923
        
        var error = await _qualityService.CalculateCalibrationErrorAsync();
        Assert.Equal(0.2923, Math.Round(error, 4));
    }

    [Fact]
    public async Task GetCategoryPerformance_WorksCorrectly()
    {
        await SeedDataAsync();

        var categories = await _categoryPerformanceService.GetCategoryPerformanceAsync();
        
        var sports = categories.First(c => c.Category == "Sports");
        Assert.Equal(7, sports.PredictionCount); // 6 + 1 old
        Assert.Equal(100m, sports.AccuracyPercentage);

        var crypto = categories.First(c => c.Category == "Crypto");
        Assert.Equal(6, crypto.PredictionCount);
        Assert.Equal(0m, crypto.AccuracyPercentage);
    }

    [Fact]
    public async Task GenerateRecommendations_ProducesCorrectWarnings()
    {
        await SeedDataAsync();

        var recommendations = await _improvementService.GenerateRecommendationsAsync();

        Assert.NotEmpty(recommendations);
        
        // Sports should have a positive recommendation because it has 100% accuracy and >= 5 predictions
        Assert.Contains(recommendations, r => r.Category == "Category Weights" && r.Message.Contains("Sports") && r.Message.Contains("outperform"));
        
        // Crypto should have a negative recommendation because it has 0% accuracy and >= 5 predictions
        Assert.Contains(recommendations, r => r.Category == "Category Weights" && r.Message.Contains("Crypto") && r.Message.Contains("poor accuracy"));
        
        // Trend should be negative because recent accuracy (50%) is worse than historical (100%)
        Assert.Contains(recommendations, r => r.Category == "Trend" && r.Message.Contains("deteriorated"));
    }
}
