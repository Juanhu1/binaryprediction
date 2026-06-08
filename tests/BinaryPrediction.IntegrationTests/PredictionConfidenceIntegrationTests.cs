using BinaryPrediction.Core.Entities;
using BinaryPrediction.Infrastructure.Persistence;
using BinaryPrediction.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BinaryPrediction.IntegrationTests;

public class PredictionConfidenceIntegrationTests : IAsyncLifetime
{
    private BinaryPredictionDbContext _dbContext = null!;
    private PredictionStatisticsService _statisticsService = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<BinaryPredictionDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new BinaryPredictionDbContext(options);
        
        await _dbContext.Database.EnsureCreatedAsync();

        _statisticsService = new PredictionStatisticsService(_dbContext);
    }

    public Task DisposeAsync()
    {
        _dbContext.Dispose();
        return Task.CompletedTask;
    }



    [Fact]
    public async Task ConfidenceBand_ResolvesCorrectly()
    {
        // Arrange
        var marketId = Guid.NewGuid();
        var market = new Market { Id = marketId, Question = "Test", Probability = 50m, EndDate = DateTime.UtcNow.AddDays(1) };
        _dbContext.Markets.Add(market);

        var predictions = new[]
        {
            new Prediction { Id = Guid.NewGuid(), MarketId = marketId, AnalysisId = Guid.NewGuid(), PredictedOutcome = "Yes", ConfidencePercentage = 95m, WasCorrect = true, EvaluatedAtUtc = DateTime.UtcNow, BrierScore = 0.05m, IsActive = true },
            new Prediction { Id = Guid.NewGuid(), MarketId = marketId, AnalysisId = Guid.NewGuid(), PredictedOutcome = "No", ConfidencePercentage = 85m, WasCorrect = false, EvaluatedAtUtc = DateTime.UtcNow, BrierScore = 0.85m, IsActive = true },
            new Prediction { Id = Guid.NewGuid(), MarketId = marketId, AnalysisId = Guid.NewGuid(), PredictedOutcome = "Yes", ConfidencePercentage = 75m, WasCorrect = true, EvaluatedAtUtc = DateTime.UtcNow, BrierScore = 0.25m, IsActive = true },
            new Prediction { Id = Guid.NewGuid(), MarketId = marketId, AnalysisId = Guid.NewGuid(), PredictedOutcome = "No", ConfidencePercentage = 55m, WasCorrect = true, EvaluatedAtUtc = DateTime.UtcNow, BrierScore = 0.45m, IsActive = true }
        };

        _dbContext.Predictions.AddRange(predictions);
        await _dbContext.SaveChangesAsync();

        // Act
        var bands = await _statisticsService.GetConfidenceBandResultsAsync();

        // Assert
        Assert.NotNull(bands);
        Assert.Equal(5, bands.Count); // Should always return 5 bands

        var band90 = bands.Single(b => b.BandName == "90-100");
        Assert.Equal(1, band90.TotalPredictions);
        Assert.Equal(1, band90.CorrectPredictions);
        Assert.Equal(100m, band90.AccuracyPercentage);

        var band80 = bands.Single(b => b.BandName == "80-89");
        Assert.Equal(1, band80.TotalPredictions);
        Assert.Equal(0, band80.CorrectPredictions);
        Assert.Equal(0m, band80.AccuracyPercentage);

        var band70 = bands.Single(b => b.BandName == "70-79");
        Assert.Equal(1, band70.TotalPredictions);
        Assert.Equal(1, band70.CorrectPredictions);
        Assert.Equal(100m, band70.AccuracyPercentage);

        var band60 = bands.Single(b => b.BandName == "60-69");
        Assert.Equal(0, band60.TotalPredictions);

        var band50 = bands.Single(b => b.BandName == "50-59");
        Assert.Equal(1, band50.TotalPredictions);
        Assert.Equal(1, band50.CorrectPredictions);
        Assert.Equal(100m, band50.AccuracyPercentage);
    }
}
