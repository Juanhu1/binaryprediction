using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BinaryPrediction.Infrastructure.Services;

public class PipelineMonitoringService : IPipelineMonitoringService
{
    private readonly BinaryPredictionDbContext _dbContext;

    public PipelineMonitoringService(BinaryPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PipelineStatusDto> GetPipelineStatusAsync(CancellationToken cancellationToken = default)
    {
        var totalMarkets = await _dbContext.Markets.CountAsync(cancellationToken);
        var totalAnalyses = await _dbContext.AiAnalyses.CountAsync(cancellationToken);
        var totalPredictions = await _dbContext.Predictions.CountAsync(cancellationToken);
        var resolvedMarkets = await _dbContext.Markets.CountAsync(m => m.ResolvedAtUtc != null, cancellationToken);
        var evaluatedPredictions = await _dbContext.Predictions.CountAsync(p => p.EvaluatedAtUtc != null, cancellationToken);

        // Calculate Missing Predictions (Analyses that do not have a corresponding Prediction)
        var analysesWithoutPredictions = await _dbContext.AiAnalyses
            .Where(a => !_dbContext.Predictions.Any(p => p.AnalysisId == a.Id))
            .CountAsync(cancellationToken);

        return new PipelineStatusDto
        {
            TotalMarkets = totalMarkets,
            TotalAnalyses = totalAnalyses,
            TotalPredictions = totalPredictions,
            AnalysesWithoutPredictions = analysesWithoutPredictions,
            ResolvedMarkets = resolvedMarkets,
            EvaluatedPredictions = evaluatedPredictions
        };
    }
}
