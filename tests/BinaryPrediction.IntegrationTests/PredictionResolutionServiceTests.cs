using System;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using System.Threading.Tasks;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using BinaryPrediction.Infrastructure.Repositories;
using BinaryPrediction.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BinaryPrediction.IntegrationTests
{
    public class PredictionResolutionServiceTests
    {
        private BinaryPredictionDbContext GetInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<BinaryPredictionDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            var context = new BinaryPredictionDbContext(options);
            return context;
        }

        private async Task SeedData(BinaryPredictionDbContext ctx, decimal confidence, string actualOutcome)
        {
            var market = new Market
            {
                Id = Guid.NewGuid(),
                Question = "Test market",
                ActualOutcome = actualOutcome,
                ResolvedAtUtc = DateTimeOffset.UtcNow
            };
            await ctx.Markets.AddAsync(market);
            var prediction = new Prediction
            {
                Id = Guid.NewGuid(),
                MarketId = market.Id,
                ConfidencePercentage = confidence,
                PredictedOutcome = confidence >= 50 ? "Yes" : "No",
                Market = market
            };
            await ctx.Predictions.AddAsync(prediction);
            await ctx.SaveChangesAsync();
        }

        [Fact]
        public async Task MarketYesOutcome_CorrectPrediction()
        {
            var ctx = GetInMemoryContext("YesOutcome");
            await SeedData(ctx, 80m, "Yes");

            var repo = new PredictionResolutionRepository(ctx);
            var service = new PredictionResolutionService(repo, NullLogger<PredictionResolutionService>.Instance);

            var processed = await service.ProcessPendingPredictionsAsync();
            Assert.Equal(1, processed);

            var pred = await ctx.Predictions.FirstAsync();
            Assert.True(pred.WasCorrect);
            Assert.Equal(0.04m, pred.BrierScore);
        }

        [Fact]
        public async Task MarketNoOutcome_IncorrectPrediction()
        {
            var ctx = GetInMemoryContext("NoOutcome");
            await SeedData(ctx, 80m, "No");

            var repo = new PredictionResolutionRepository(ctx);
            var service = new PredictionResolutionService(repo, NullLogger<PredictionResolutionService>.Instance);

            var processed = await service.ProcessPendingPredictionsAsync();
            Assert.Equal(1, processed);

            var pred = await ctx.Predictions.FirstAsync();
            Assert.False(pred.WasCorrect);
            // predicted Yes with 0.8 probability, actual No => actualYesValue = 0
            Assert.Equal(0.64m, pred.BrierScore);
        }

        [Fact]
        public async Task DuplicateProcessing_SkipsAlreadyEvaluated()
        {
            var ctx = GetInMemoryContext("Duplicate");
            await SeedData(ctx, 80m, "Yes");

            var repo = new PredictionResolutionRepository(ctx);
            var service = new PredictionResolutionService(repo, NullLogger<PredictionResolutionService>.Instance);

            // first run
            var first = await service.ProcessPendingPredictionsAsync();
            Assert.Equal(1, first);

            // second run should process 0 predictions because EvaluatedAtUtc is set
            var second = await service.ProcessPendingPredictionsAsync();
            Assert.Equal(0, second);
        }
    }
}
