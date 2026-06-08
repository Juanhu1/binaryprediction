using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using BinaryPrediction.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace BinaryPrediction.IntegrationTests;

public class PredictionEvaluationIntegrationTests
{
    private readonly IPredictionEvaluationService _evaluationService;
    private readonly BinaryPredictionDbContext _dbContext;

    public PredictionEvaluationIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<BinaryPredictionDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new BinaryPredictionDbContext(options);
        
        _dbContext.Database.EnsureDeleted();
        _dbContext.Database.EnsureCreated();

        var predictionRepository = new BinaryPrediction.Infrastructure.Persistence.Repositories.PredictionRepository(_dbContext);
        var logger = new Mock<ILogger<PredictionEvaluationService>>().Object;

        _evaluationService = new PredictionEvaluationService(predictionRepository, logger);
    }

    private async Task<(Prediction, Market)> CreateTestPredictionAsync(string predictedOutcome, decimal confidence)
    {
        var market = new Market { Id = Guid.NewGuid(), Question = "Test Market", Slug = "test" };
        var analysis = new AiAnalysis { Id = Guid.NewGuid(), MarketId = market.Id };
        
        var prediction = new Prediction
        {
            Id = Guid.NewGuid(),
            MarketId = market.Id,
            AnalysisId = analysis.Id,
            PredictedOutcome = predictedOutcome,
            ConfidencePercentage = confidence,
            ReasoningSummary = "Test",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsActive = true
        };

        _dbContext.Markets.Add(market);
        _dbContext.AiAnalyses.Add(analysis);
        _dbContext.Predictions.Add(prediction);
        await _dbContext.SaveChangesAsync();

        return (prediction, market);
    }

    [Fact]
    public async Task Scenario1_PredictedYes_ActualYes_CalculatesCorrectly()
    {
        var (prediction, market) = await CreateTestPredictionAsync("Yes", 80m); // P = 0.8
        
        await _evaluationService.EvaluateMarketPredictionsAsync(market, "Yes", CancellationToken.None);

        var evaluated = await _dbContext.Predictions.FindAsync(prediction.Id);
        Assert.NotNull(evaluated);
        Assert.True(evaluated.WasCorrect);
        Assert.NotNull(evaluated.EvaluatedAtUtc);
        
        // P=0.8, Actual=1 => (0.8 - 1)^2 = 0.04
        Assert.Equal(0.04m, evaluated.BrierScore);
    }

    [Fact]
    public async Task Scenario2_PredictedYes_ActualNo_CalculatesCorrectly()
    {
        var (prediction, market) = await CreateTestPredictionAsync("Yes", 80m); // P = 0.8
        
        await _evaluationService.EvaluateMarketPredictionsAsync(market, "No", CancellationToken.None);

        var evaluated = await _dbContext.Predictions.FindAsync(prediction.Id);
        Assert.NotNull(evaluated);
        Assert.False(evaluated.WasCorrect);
        Assert.NotNull(evaluated.EvaluatedAtUtc);
        
        // P=0.8, Actual=0 => (0.8 - 0)^2 = 0.64
        Assert.Equal(0.64m, evaluated.BrierScore);
    }

    [Fact]
    public async Task Scenario3_PredictedNo_ActualNo_CalculatesCorrectly()
    {
        var (prediction, market) = await CreateTestPredictionAsync("No", 70m); // P(No)=0.7 => P(Yes)=0.3
        
        await _evaluationService.EvaluateMarketPredictionsAsync(market, "No", CancellationToken.None);

        var evaluated = await _dbContext.Predictions.FindAsync(prediction.Id);
        Assert.NotNull(evaluated);
        Assert.True(evaluated.WasCorrect);
        Assert.NotNull(evaluated.EvaluatedAtUtc);
        
        // P(Yes)=0.3, Actual=0 => (0.3 - 0)^2 = 0.09
        Assert.Equal(0.09m, evaluated.BrierScore);
    }

    [Fact]
    public async Task Scenario4_DuplicateResolution_DoesNotOverwrite()
    {
        var (prediction, market) = await CreateTestPredictionAsync("Yes", 80m);
        
        await _evaluationService.EvaluateMarketPredictionsAsync(market, "Yes", CancellationToken.None);
        var evaluatedFirst = await _dbContext.Predictions.AsNoTracking().FirstAsync(p => p.Id == prediction.Id);
        
        // Wait briefly to ensure timestamp would be different
        await Task.Delay(100);

        // Attempt duplicate resolution with different outcome
        await _evaluationService.EvaluateMarketPredictionsAsync(market, "No", CancellationToken.None);
        var evaluatedSecond = await _dbContext.Predictions.AsNoTracking().FirstAsync(p => p.Id == prediction.Id);

        // Should not have overwritten
        Assert.Equal(evaluatedFirst.EvaluatedAtUtc, evaluatedSecond.EvaluatedAtUtc);
        Assert.Equal(evaluatedFirst.WasCorrect, evaluatedSecond.WasCorrect);
        Assert.Equal(evaluatedFirst.BrierScore, evaluatedSecond.BrierScore);
        Assert.True(evaluatedSecond.WasCorrect); // Still true from first evaluation
    }

    [Fact]
    public async Task Scenario5_UnknownOutcome_DoesNotEvaluate()
    {
        var (prediction, market) = await CreateTestPredictionAsync("Yes", 80m);
        
        await _evaluationService.EvaluateMarketPredictionsAsync(market, "Maybe", CancellationToken.None);

        var evaluated = await _dbContext.Predictions.FindAsync(prediction.Id);
        Assert.NotNull(evaluated);
        Assert.Null(evaluated.WasCorrect);
        Assert.Null(evaluated.EvaluatedAtUtc);
        Assert.Null(evaluated.BrierScore);
    }
}
