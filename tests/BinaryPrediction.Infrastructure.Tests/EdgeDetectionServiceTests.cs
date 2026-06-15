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
        Assert.Equal(aiConfidence, opportunity.ConfidencePercentage);
        Assert.Equal(expectedGap * aiConfidence, opportunity.EdgeScore);
    }

    [Fact]
    public async Task GetOpportunitiesAsync_SupportsSorting()
    {
        // Arrange
        var market1 = new Market { Id = Guid.NewGuid(), Question = "M1", Probability = 0.50m };
        var market2 = new Market { Id = Guid.NewGuid(), Question = "M2", Probability = 0.50m };
        var market3 = new Market { Id = Guid.NewGuid(), Question = "M3", Probability = 0.50m };
        _dbContext.Markets.AddRange(market1, market2, market3);

        var pred1 = new Prediction { Id = Guid.NewGuid(), MarketId = market1.Id, ConfidencePercentage = 60m, AiProbability = 60m, PromptVersionUsed = "v2" };
        var pred2 = new Prediction { Id = Guid.NewGuid(), MarketId = market2.Id, ConfidencePercentage = 80m, AiProbability = 70m, PromptVersionUsed = "v2" };
        var pred3 = new Prediction { Id = Guid.NewGuid(), MarketId = market3.Id, ConfidencePercentage = 70m, AiProbability = 80m, PromptVersionUsed = "v2" };
        _dbContext.Predictions.AddRange(pred1, pred2, pred3);
        await _dbContext.SaveChangesAsync();

        await _service.DetectOpportunityAsync(pred1.Id);
        await _service.DetectOpportunityAsync(pred2.Id);
        await _service.DetectOpportunityAsync(pred3.Id);

        var dashboardService = new DashboardService(_dbContext, new LoggerFactory().CreateLogger<DashboardService>());

        // Act 1: default sort (edgescore desc)
        var queryDefault = new BinaryPrediction.Core.DTOs.Dashboard.DashboardOpportunityQuery { Page = 1, PageSize = 10 };
        var resultDefault = await dashboardService.GetOpportunitiesAsync(queryDefault);
        
        // Assert: order should be Opp3 (2100), Opp2 (1600), Opp1 (600)
        Assert.Equal(3, resultDefault.Items.Count);
        Assert.Equal(pred3.Id, resultDefault.Items[0].PredictionId);
        Assert.Equal(pred2.Id, resultDefault.Items[1].PredictionId);
        Assert.Equal(pred1.Id, resultDefault.Items[2].PredictionId);

        // Act 2: sort by confidence desc
        var queryConfidenceDesc = new BinaryPrediction.Core.DTOs.Dashboard.DashboardOpportunityQuery { SortBy = "confidence", SortDesc = true, Page = 1, PageSize = 10 };
        var resultConfidenceDesc = await dashboardService.GetOpportunitiesAsync(queryConfidenceDesc);

        // Assert: order should be Opp2 (80), Opp3 (70), Opp1 (60)
        Assert.Equal(pred2.Id, resultConfidenceDesc.Items[0].PredictionId);
        Assert.Equal(pred3.Id, resultConfidenceDesc.Items[1].PredictionId);
        Assert.Equal(pred1.Id, resultConfidenceDesc.Items[2].PredictionId);

        // Act 3: sort by gap asc
        var queryGapAsc = new BinaryPrediction.Core.DTOs.Dashboard.DashboardOpportunityQuery { SortBy = "gap", SortDesc = false, Page = 1, PageSize = 10 };
        var resultGapAsc = await dashboardService.GetOpportunitiesAsync(queryGapAsc);

        // Assert: order should be Opp1 (10), Opp2 (20), Opp3 (30)
        Assert.Equal(pred1.Id, resultGapAsc.Items[0].PredictionId);
        Assert.Equal(pred2.Id, resultGapAsc.Items[1].PredictionId);
        Assert.Equal(pred3.Id, resultGapAsc.Items[2].PredictionId);
    }

    [Fact]
    public async Task GetOpportunitiesAsync_DeduplicatesAndReturnsLatestPerMarket_AndCalculatesSummaryMetrics()
    {
        // Arrange
        var market = new Market { Id = Guid.NewGuid(), Question = "Duplicate Market Test", Probability = 0.50m };
        _dbContext.Markets.Add(market);

        var pred1 = new Prediction 
        { 
            Id = Guid.NewGuid(), 
            MarketId = market.Id, 
            ConfidencePercentage = 60m, 
            AiProbability = 60m, 
            PromptVersionUsed = "v2" 
        };
        _dbContext.Predictions.Add(pred1);
        await _dbContext.SaveChangesAsync();

        await _service.DetectOpportunityAsync(pred1.Id);

        // Second prediction (newer opportunity)
        var pred2 = new Prediction 
        { 
            Id = Guid.NewGuid(), 
            MarketId = market.Id, 
            ConfidencePercentage = 80m, 
            AiProbability = 80m, 
            PromptVersionUsed = "v2" 
        };
        _dbContext.Predictions.Add(pred2);
        await _dbContext.SaveChangesAsync();

        await _service.DetectOpportunityAsync(pred2.Id);

        var dashboardService = new DashboardService(_dbContext, new LoggerFactory().CreateLogger<DashboardService>());

        // Act
        var query = new BinaryPrediction.Core.DTOs.Dashboard.DashboardOpportunityQuery { Page = 1, PageSize = 10 };
        var result = await dashboardService.GetOpportunitiesAsync(query);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(pred2.Id, result.Items[0].PredictionId);

        // Verify summary metrics
        Assert.Equal(2, result.TotalOpportunityRecords);
        Assert.Equal(1, result.UniqueMarketsWithOpportunities);
        Assert.Equal(1, result.CurrentActiveOpportunities);
    }
}
