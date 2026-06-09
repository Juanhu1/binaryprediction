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
        
        var optionsMock = new Mock<Microsoft.Extensions.Options.IOptions<BinaryPrediction.Core.Common.OpenAiSettings>>();
        optionsMock.Setup(x => x.Value).Returns(new BinaryPrediction.Core.Common.OpenAiSettings { Model = "test-model" });

        _predictionService = new PredictionService(
            _mockOpenAiService.Object,
            _predictionRepository,
            NullLogger<PredictionService>.Instance,
            _dbContext,
            optionsMock.Object
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
                ConfidencePercentage = 95m,
                ReasoningSummary = "Strong edge detected."
            });

        // Act
        var result = await _predictionService.CreatePredictionAsync(analysis, market);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Yes", result.PredictedOutcome);
        Assert.Equal(95m, result.ConfidencePercentage);
        Assert.True(result.IsActive);
        
        var savedPredictions = await _dbContext.Predictions.ToListAsync();
        Assert.Single(savedPredictions);
        Assert.Equal(result.Id, savedPredictions[0].Id);
    }

    [Fact]
    public async Task CreatePredictionAsync_SkipsIfPredictionExistsForAnalysis()
    {
        // Arrange
        var marketId = Guid.NewGuid();
        var market = new Market { Id = marketId, Question = "Test", Probability = 50m, EndDate = DateTime.UtcNow.AddDays(1) };
        var analysisId = Guid.NewGuid();
        var newAnalysis = new AiAnalysis { Id = analysisId, MarketId = marketId, EstimatedProbability = 90m, Confidence = 90m, Summary = "New" };

        _dbContext.Markets.Add(market);
        _dbContext.AiAnalyses.Add(newAnalysis);
        await _dbContext.SaveChangesAsync();

        // Add old prediction manually FOR THIS ANALYSIS
        var oldPrediction = new Prediction
        {
            Id = Guid.NewGuid(),
            MarketId = marketId,
            AnalysisId = analysisId,
            PredictedOutcome = "No",
            ConfidencePercentage = 50m,
            IsActive = true
        };
        _dbContext.Predictions.Add(oldPrediction);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _predictionService.CreatePredictionAsync(newAnalysis, market);

        // Assert
        Assert.Null(result); // Skipped because prediction for THIS analysis exists

        var allPredictions = await _dbContext.Predictions.Where(p => p.MarketId == marketId).ToListAsync();
        Assert.Single(allPredictions);
        Assert.Equal(oldPrediction.Id, allPredictions[0].Id);
    }
}
