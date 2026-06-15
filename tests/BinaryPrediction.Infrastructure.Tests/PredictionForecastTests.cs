using System;
using System.Threading;
using System.Threading.Tasks;
using BinaryPrediction.Core.Common;
using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using BinaryPrediction.Infrastructure.Persistence.Repositories;
using BinaryPrediction.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BinaryPrediction.Infrastructure.Tests;

public class PredictionForecastTests
{
    private readonly BinaryPredictionDbContext _dbContext;
    private readonly PredictionRepository _predictionRepository;
    private readonly DummyOpenAiAnalysisService _dummyOpenAiService;
    private readonly DummyEdgeDetectionService _dummyEdgeDetectionService;
    private readonly PredictionService _predictionService;

    public PredictionForecastTests()
    {
        var options = new DbContextOptionsBuilder<BinaryPredictionDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new BinaryPredictionDbContext(options);

        _predictionRepository = new PredictionRepository(_dbContext);
        _dummyOpenAiService = new DummyOpenAiAnalysisService();
        _dummyEdgeDetectionService = new DummyEdgeDetectionService();

        var openAiOptions = Options.Create(new OpenAiSettings { Model = "test-model" });

        _predictionService = new PredictionService(
            _dummyOpenAiService,
            _predictionRepository,
            NullLogger<PredictionService>.Instance,
            _dummyEdgeDetectionService,
            _dbContext,
            openAiOptions
        );
    }

    [Fact]
    public async Task Case1_LowProbability_HighConfidence_PredictsNo()
    {
        // Arrange
        var market = new Market { Id = Guid.NewGuid(), Question = "Will Event Occur?", Probability = 0.5m };
        var analysis = new AiAnalysis 
        { 
            Id = Guid.NewGuid(), 
            MarketId = market.Id,
            EstimatedProbability = 12m,
            Confidence = 88m,
            Summary = "Reasoning",
            PromptVersion = "v2"
        };
        
        _dbContext.Markets.Add(market);
        _dbContext.AiAnalyses.Add(analysis);
        await _dbContext.SaveChangesAsync();

        // Act
        var prediction = await _predictionService.CreatePredictionAsync(analysis, market);

        // Assert
        Assert.NotNull(prediction);
        Assert.Equal("No", prediction.PredictedOutcome);
        Assert.Equal(12m, prediction.AiProbability);
        Assert.Equal(88m, prediction.ConfidencePercentage);
    }

    [Fact]
    public async Task Case2_HighProbability_ModerateConfidence_PredictsYes()
    {
        // Arrange
        var market = new Market { Id = Guid.NewGuid(), Question = "Will Event Occur?", Probability = 0.5m };
        var analysis = new AiAnalysis 
        { 
            Id = Guid.NewGuid(), 
            MarketId = market.Id,
            EstimatedProbability = 67m,
            Confidence = 74m,
            Summary = "Reasoning",
            PromptVersion = "v2"
        };

        _dbContext.Markets.Add(market);
        _dbContext.AiAnalyses.Add(analysis);
        await _dbContext.SaveChangesAsync();

        // Act
        var prediction = await _predictionService.CreatePredictionAsync(analysis, market);

        // Assert
        Assert.NotNull(prediction);
        Assert.Equal("Yes", prediction.PredictedOutcome);
        Assert.Equal(67m, prediction.AiProbability);
        Assert.Equal(74m, prediction.ConfidencePercentage);
    }

    [Fact]
    public async Task Case3_ConfidenceChanges_EventProbabilityStaysConstant_AiProbabilityRemainsUnchanged()
    {
        // Arrange
        var market = new Market { Id = Guid.NewGuid(), Question = "Will Event Occur?", Probability = 0.5m };
        
        // Setup prediction 1 (Confidence = 88)
        var analysis1 = new AiAnalysis 
        { 
            Id = Guid.NewGuid(), 
            MarketId = market.Id,
            EstimatedProbability = 45m,
            Confidence = 88m,
            Summary = "Reasoning",
            PromptVersion = "v2"
        };
        _dbContext.Markets.Add(market);
        _dbContext.AiAnalyses.Add(analysis1);
        await _dbContext.SaveChangesAsync();

        var prediction1 = await _predictionService.CreatePredictionAsync(analysis1, market);

        // Setup prediction 2 (Confidence = 60, but EventProbability is still 45)
        var analysis2 = new AiAnalysis 
        { 
            Id = Guid.NewGuid(), 
            MarketId = market.Id,
            EstimatedProbability = 45m,
            Confidence = 60m,
            Summary = "Reasoning",
            PromptVersion = "v2"
        };
        _dbContext.AiAnalyses.Add(analysis2);
        await _dbContext.SaveChangesAsync();

        var prediction2 = await _predictionService.CreatePredictionAsync(analysis2, market);

        // Assert
        Assert.NotNull(prediction1);
        Assert.NotNull(prediction2);
        Assert.Equal(45m, prediction1.AiProbability);
        Assert.Equal(45m, prediction2.AiProbability);
        Assert.Equal(88m, prediction1.ConfidencePercentage);
        Assert.Equal(60m, prediction2.ConfidencePercentage);
    }
}

public class DummyOpenAiAnalysisService : IOpenAiAnalysisService
{
    public AiPredictionResultDto? ResultToReturn { get; set; }

    public Task<AiAnalysisResultDto?> AnalyzeMarketAsync(Market market, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<AiAnalysisResultDto?>(null);
    }

    public Task<AiPredictionResultDto?> GeneratePredictionAsync(Market market, AiAnalysis analysis, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ResultToReturn);
    }
}

public class DummyEdgeDetectionService : IEdgeDetectionService
{
    public Task DetectOpportunityAsync(Guid predictionId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
