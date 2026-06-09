using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BinaryPrediction.Infrastructure.Services;

public class AccuracyMonitoringService : IAccuracyMonitoringService
{
    private readonly BinaryPredictionDbContext _dbContext;

    public AccuracyMonitoringService(BinaryPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AccuracyStatusDto> GetAccuracyStatusAsync(CancellationToken cancellationToken = default)
    {
        var evaluatedPredictions = await _dbContext.Predictions
            .Where(p => p.EvaluatedAtUtc != null)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var total = evaluatedPredictions.Count;
        if (total == 0)
        {
            return new AccuracyStatusDto();
        }

        var correct = evaluatedPredictions.Count(e => e.WasCorrect == true);
        var incorrect = evaluatedPredictions.Count(e => e.WasCorrect == false);
        var avgBrier = evaluatedPredictions.Where(e => e.BrierScore.HasValue).Average(e => e.BrierScore!.Value);

        return new AccuracyStatusDto
        {
            PredictionsEvaluated = total,
            CorrectPredictions = correct,
            IncorrectPredictions = incorrect,
            AccuracyPercentage = Math.Round((decimal)correct / total * 100m, 2),
            AverageBrierScore = Math.Round(avgBrier, 4)
        };
    }
}
