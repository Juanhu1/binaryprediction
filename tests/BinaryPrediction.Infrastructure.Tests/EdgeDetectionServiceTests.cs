using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Core.Options;
using BinaryPrediction.Core.Enums;
using BinaryPrediction.Infrastructure.Persistence;
using BinaryPrediction.Infrastructure.Persistence.Repositories;
using BinaryPrediction.Infrastructure.Repositories;
using BinaryPrediction.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace BinaryPrediction.Infrastructure.Tests;

public class EdgeDetectionServiceTests
{
    private readonly BinaryPredictionDbContext _dbContext;
    private readonly EdgeDetectionService _service;

    public EdgeDetectionServiceTests()
    {
        var options = new DbContextOptionsBuilder<BinaryPredictionDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new BinaryPredictionDbContext(options);

        var predictionRepo = new PredictionRepository(_dbContext);
        var opportunityRepo = new PredictionOpportunityRepository(_dbContext);
        var historyRepo = new OpportunityStatusHistoryRepository(_dbContext);
        var lifecycleService = new OpportunityLifecycleService(opportunityRepo, historyRepo, new LoggerFactory().CreateLogger<OpportunityLifecycleService>());
        var edgeOptions = Microsoft.Extensions.Options.Options.Create(new EdgeDetectionOptions { GapThresholdPercentage = 5 });
        var logger = new LoggerFactory().CreateLogger<EdgeDetectionService>();

        _service = new EdgeDetectionService(predictionRepo, opportunityRepo, lifecycleService, edgeOptions, logger);
    }

    [Theory]
    [InlineData(0.9995, 30.00, "Yes", 69.95)] // Case A: Market = 99.95%, AI = 30.00%, Gap = 69.95%
    [InlineData(0.1000, 80.00, "Yes", 70.00)] // Case B: Market = 10.00%, AI = 80.00%, Gap = 70.00%
    [InlineData(0.5000, 50.00, "Yes", 0.00)]  // Case C: Market = 50.00%, AI = 50.00%, Gap = 0.00%
    [InlineData(0.0050, 99.50, "Yes", 99.00)] // Case D: Market = 0.50%, AI = 99.50%, Gap = 99.00%
    public async Task DetectOpportunity_CalculatesProbabilityGapAndScaleCorrectly(
        decimal marketProbabilityRaw,
        decimal aiConfidence,
        string predictedOutcome,
        decimal expectedGap)
    {
        // Arrange
        var market = new Market
        {
            Id = Guid.NewGuid(),
            Question = "Test Market Question",
            Probability = marketProbabilityRaw
        };
        _dbContext.Markets.Add(market);

        var prediction = new Prediction
        {
            Id = Guid.NewGuid(),
            MarketId = market.Id,
            ConfidencePercentage = aiConfidence,
            PredictedOutcome = predictedOutcome,
            IsActive = true
        };
        _dbContext.Predictions.Add(prediction);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.DetectOpportunityAsync(prediction.Id);

        // Assert
        var opportunity = await _dbContext.PredictionOpportunities.FirstOrDefaultAsync(o => o.PredictionId == prediction.Id);
        Assert.NotNull(opportunity);
        Assert.Equal(expectedGap, opportunity.ProbabilityGap, precision: 2);
        Assert.Equal(marketProbabilityRaw * 100m, opportunity.MarketProbability, precision: 2);
        Assert.Equal(aiConfidence, opportunity.AiProbability, precision: 2);
    }
}
