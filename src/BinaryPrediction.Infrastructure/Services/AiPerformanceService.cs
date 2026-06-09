using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BinaryPrediction.Infrastructure.Services;

public class AiPerformanceService : IAiPerformanceService
{
    private readonly BinaryPredictionDbContext _dbContext;

    public AiPerformanceService(BinaryPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AiPerformanceDto> GetPerformanceAsync(CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.AiUsageRecords.AsNoTracking().ToListAsync(cancellationToken);

        var analyses = records.Where(r => r.OperationType == "Analysis").ToList();
        var predictions = records.Where(r => r.OperationType == "Prediction").ToList();

        var dto = new AiPerformanceDto
        {
            AnalysesGenerated = analyses.Count(a => a.IsSuccess),
            FailedAnalyses = analyses.Count(a => !a.IsSuccess),
            PredictionsGenerated = predictions.Count(p => p.IsSuccess),
            FailedPredictions = predictions.Count(p => !p.IsSuccess),
            AverageLatencyMs = records.Any() ? records.Average(r => r.LatencyMs) : 0,
            AverageCostUsd = records.Any() ? records.Average(r => r.EstimatedCostUsd) : 0
        };

        return dto;
    }
}
