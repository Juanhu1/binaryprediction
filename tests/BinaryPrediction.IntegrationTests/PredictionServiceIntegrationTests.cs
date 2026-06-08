using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using BinaryPrediction.Infrastructure.Persistence.Repositories;
using BinaryPrediction.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BinaryPrediction.IntegrationTests;

public class PredictionServiceIntegrationTests
{
    private readonly BinaryPredictionDbContext _dbContext;
    private readonly IPredictionRepository _predictionRepository;
    private readonly Mock<IOpenAiAnalysisService> _mockOpenAiService;
    private readonly IPredictionService _predictionService;

    public PredictionServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<BinaryPredictionDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per test instance
            .Options;

        _dbContext = new BinaryPredictionDbContext(options);
        
        // Ensure database is clean
        _dbContext.Database.EnsureDeleted();
        _dbContext.Database.EnsureCreated();

        _predictionRepository = new PredictionRepository(_dbContext);
        
        _mockOpenAiService = new Mock<IOpenAiAnalysisService>();
        
        _predictionService = new PredictionService(
            _mockOpenAiService.Object,
            _predictionRepository,
            NullLogger<PredictionService>.Instance
        );
    }

    [Fact]
    public async Task CreatePredictionAsync_SavesPrediction_And_ActivatesIt()
    {
        // Arrange
        var marketId = Guid.NewGuid();
        var market = new Market { Id = marketId, Question = "Test Question", Probability = 50m, EndDate = DateTime.UtcNow.AddDays(1) };
        var analysis = new AiAnalysis { Id = Guid.NewGuid(), MarketId = marketId, EstimatedProbability = 80m, Confidence = 90m, Edge = 30m, Summary = "Summary" };

        _dbContext.Markets.Add(market);
        _dbContext.AiAnalyses.Add(analysis);
        await _dbContext.SaveChangesAsync();

        _mockOpenAiService.Setup(s => s.GeneratePredictionAsync(It.IsAny<Market>(), It.IsAny<AiAnalysis>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiPredictionResultDto
            {
                PredictedOutcome = "Yes",
                ConfidenceScore = 95m,
                ReasoningSummary = "Strong edge detected."
            });

        // Act
        var result = await _predictionService.CreatePredictionAsync(analysis, market);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Yes", result.PredictedOutcome);
        Assert.Equal(95m, result.ConfidenceScore);
        Assert.True(result.IsActive);
        
        var savedPredictions = await _dbContext.Predictions.ToListAsync();
        Assert.Single(savedPredictions);
        Assert.Equal(result.Id, savedPredictions[0].Id);
    }

    [Fact]
    public async Task CreatePredictionAsync_DeactivatesPreviousPredictions()
    {
        // Arrange
        var marketId = Guid.NewGuid();
        var market = new Market { Id = marketId, Question = "Test", Probability = 50m, EndDate = DateTime.UtcNow.AddDays(1) };
        var oldAnalysis = new AiAnalysis { Id = Guid.NewGuid(), MarketId = marketId, EstimatedProbability = 50m, Confidence = 50m, Summary = "Old" };
        var newAnalysis = new AiAnalysis { Id = Guid.NewGuid(), MarketId = marketId, EstimatedProbability = 90m, Confidence = 90m, Summary = "New" };

        _dbContext.Markets.Add(market);
        _dbContext.AiAnalyses.AddRange(oldAnalysis, newAnalysis);
        await _dbContext.SaveChangesAsync();

        // Add old prediction manually
        var oldPrediction = new Prediction
        {
            Id = Guid.NewGuid(),
            MarketId = marketId,
            AnalysisId = oldAnalysis.Id,
            PredictedOutcome = "No",
            ConfidenceScore = 50m,
            IsActive = true
        };
        _dbContext.Predictions.Add(oldPrediction);
        await _dbContext.SaveChangesAsync();

        // Setup mock for new prediction
        _mockOpenAiService.Setup(s => s.GeneratePredictionAsync(It.IsAny<Market>(), It.IsAny<AiAnalysis>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiPredictionResultDto
            {
                PredictedOutcome = "Yes",
                ConfidenceScore = 90m,
                ReasoningSummary = "New reasoning."
            });

        // Act
        var result = await _predictionService.CreatePredictionAsync(newAnalysis, market);

        // Assert
        var allPredictions = await _dbContext.Predictions.Where(p => p.MarketId == marketId).ToListAsync();
        Assert.Equal(2, allPredictions.Count);

        var dbOldPrediction = allPredictions.First(p => p.Id == oldPrediction.Id);
        var dbNewPrediction = allPredictions.First(p => p.Id == result.Id);

        Assert.False(dbOldPrediction.IsActive);
        Assert.NotNull(dbOldPrediction.ExpiresAtUtc);

        Assert.True(dbNewPrediction.IsActive);
        Assert.Null(dbNewPrediction.ExpiresAtUtc);
    }
}
