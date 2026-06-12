using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BinaryPrediction.Api.Controllers;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using BinaryPrediction.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Microsoft.Extensions.Logging;

namespace BinaryPrediction.Api.Tests;

public class PerformanceControllerTests
{
    private readonly BinaryPredictionDbContext _dbContext;
    private readonly IPredictionPerformanceService _performanceService;
    private readonly PerformanceController _controller;

    public PerformanceControllerTests()
    {
        var options = new DbContextOptionsBuilder<BinaryPredictionDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new BinaryPredictionDbContext(options);

        // Seed predictions with evaluation data
        var now = DateTimeOffset.UtcNow;
        var predictions = new List<Prediction>
        {
            new Prediction
            {
                Id = Guid.NewGuid(),
                MarketId = Guid.NewGuid(),
                ConfidencePercentage = 70m,
                EvaluatedAtUtc = now,
                WasCorrect = true,
                BrierScore = 0.09m,
                PredictionError = 0.1m,
                ActualOutcome = "Yes"
            },
            new Prediction
            {
                Id = Guid.NewGuid(),
                MarketId = Guid.NewGuid(),
                ConfidencePercentage = 30m,
                EvaluatedAtUtc = now,
                WasCorrect = false,
                BrierScore = 0.4m,
                PredictionError = 0.5m,
                ActualOutcome = "No"
            }
        };
        _dbContext.Predictions.AddRange(predictions);
        _dbContext.SaveChanges();

        _performanceService = new PredictionPerformanceService(_dbContext);
        // Generate snapshot for today
        _performanceService.GenerateDailySnapshotAsync().GetAwaiter().GetResult();

        _controller = new PerformanceController(_performanceService, new LoggerFactory().CreateLogger<PerformanceController>());
    }

    [Fact]
    public async Task GetCurrentPerformance_ReturnsAggregatedMetrics()
    {
        var result = await _controller.GetCurrentPerformance(CancellationToken.None) as OkObjectResult;
        Assert.NotNull(result);
        var dto = result.Value as CurrentPerformanceDto;
        Assert.NotNull(dto);
        Assert.Equal(2, dto.TotalPredictions);
        Assert.Equal(1, dto.CorrectPredictions);
        Assert.Equal(50m, dto.AccuracyPercentage);
        Assert.Equal(50m, dto.AverageConfidence); // (70+30)/2
        Assert.Equal(0.245m, dto.AverageBrierScore); // (0.09+0.4)/2
        Assert.Equal(0.3m, dto.AveragePredictionError); // (0.1+0.5)/2
    }
}
