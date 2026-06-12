using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using BinaryPrediction.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BinaryPrediction.Infrastructure.Tests;

public class PredictionEvaluationServiceTests
{
    private readonly BinaryPredictionDbContext _dbContext;
    private readonly PredictionEvaluationService _service;

    public PredictionEvaluationServiceTests()
    {
        var options = new DbContextOptionsBuilder<BinaryPredictionDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new BinaryPredictionDbContext(options);

        var logger = new LoggerFactory().CreateLogger<PredictionEvaluationService>();
        _service = new PredictionEvaluationService(_dbContext, logger);
    }

    [Fact]
    public async Task EvaluateMarketPredictions_PersistsActualOutcomeAndError()
    {
        // Arrange market
        var market = new Market
        {
            Id = Guid.NewGuid(),
            ActualOutcome = "Yes",
            ResolvedAtUtc = DateTimeOffset.UtcNow,
        };
        _dbContext.Markets.Add(market);

        // Predictions without evaluation data
        var predictions = new List<Prediction>
        {
            new Prediction
            {
                Id = Guid.NewGuid(),
                MarketId = market.Id,
                ConfidencePercentage = 80m,
                PredictedOutcome = "Yes"
            },
            new Prediction
            {
                Id = Guid.NewGuid(),
                MarketId = market.Id,
                ConfidencePercentage = 30m,
                PredictedOutcome = "No"
            }
        };
        _dbContext.Predictions.AddRange(predictions);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.EvaluateMarketPredictionsAsync(market, market.ActualOutcome!);

        // Assert
        var evaluated = await _dbContext.Predictions.ToListAsync();
        Assert.All(evaluated, p => Assert.NotNull(p.ActualOutcome));
        Assert.All(evaluated, p => Assert.NotNull(p.PredictionError));
        // Verify at least one error is non-zero (different confidence yields error)
        Assert.Contains(evaluated, p => p.PredictionError > 0);
    }
}
