using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BinaryPrediction.Infrastructure.Services
{
    /// <summary>
    /// Service that repairs inconsistent evaluation data and recomputes edge detection opportunities.
    /// </summary>
    public class DataRepairService : IDataRepairService
    {
        private readonly BinaryPredictionDbContext _dbContext;
        private readonly ILogger<DataRepairService> _logger;
        private readonly IEdgeDetectionService _edgeDetectionService;
        private readonly IPredictionOpportunityRepository _opportunityRepo;
        private readonly IPredictionRepository _predictionRepo;

        public DataRepairService(
            BinaryPredictionDbContext dbContext,
            ILogger<DataRepairService> logger,
            IEdgeDetectionService edgeDetectionService,
            IPredictionOpportunityRepository opportunityRepo,
            IPredictionRepository predictionRepo)
        {
            _dbContext = dbContext;
            _logger = logger;
            _edgeDetectionService = edgeDetectionService;
            _opportunityRepo = opportunityRepo;
            _predictionRepo = predictionRepo;
        }

        public async Task RepairAsync(CancellationToken cancellationToken = default)
        {
            var predictions = await _dbContext.Predictions
                .Include(p => p.Market)
                .Where(p => p.ActualOutcome != null && p.WasCorrect != null)
                .ToListAsync(cancellationToken);

            int repairedCount = 0;
            foreach (var prediction in predictions)
            {
                var actualOutcome = prediction.ActualOutcome?.Trim() ?? string.Empty;
                var confidenceProbability = prediction.ConfidencePercentage / 100m;
                var predictedOutcome = prediction.PredictedOutcome?.Trim() ?? string.Empty;
                var predictedYesProbability = predictedOutcome.Equals("Yes", StringComparison.OrdinalIgnoreCase)
                    ? confidenceProbability
                    : (1m - confidenceProbability);
                var actualYesValue = actualOutcome.Equals("Yes", StringComparison.OrdinalIgnoreCase) ? 1m : 0m;
                var correct = predictedOutcome.Equals(actualOutcome, StringComparison.OrdinalIgnoreCase);
                var error = Math.Abs(predictedYesProbability - actualYesValue);
                var brier = (predictedYesProbability - actualYesValue) * (predictedYesProbability - actualYesValue);

                bool needsUpdate = false;
                if (prediction.WasCorrect != correct)
                {
                    prediction.WasCorrect = correct;
                    needsUpdate = true;
                }
                if (prediction.PredictionError != error)
                {
                    prediction.PredictionError = error;
                    needsUpdate = true;
                }
                if (prediction.BrierScore != brier)
                {
                    prediction.BrierScore = brier;
                    needsUpdate = true;
                }
                if (needsUpdate)
                {
                    repairedCount++;
                }
            }

            if (repairedCount > 0)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Repaired {Count} prediction records with inconsistent evaluation data.", repairedCount);
            }
            else
            {
                _logger.LogInformation("No inconsistent prediction records found for repair.");
            }
        }

        public async Task RecomputeAllOpportunitiesAsync(CancellationToken cancellationToken = default)
        {
            // Delete all existing opportunity records
            await _opportunityRepo.DeleteAllAsync(cancellationToken);
            _logger.LogInformation("Deleted all existing PredictionOpportunity records.");

            // Get all active predictions (those with a market attached)
            var predictions = await _predictionRepo.GetActiveAsync(cancellationToken);
            foreach (var prediction in predictions)
            {
                try
                {
                    await _edgeDetectionService.DetectOpportunityAsync(prediction.Id, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error detecting opportunity for prediction {PredictionId}", prediction.Id);
                }
            }
            _logger.LogInformation("Recomputed opportunities for {Count} predictions.", predictions?.Count() ?? 0);
        }
    }
}
