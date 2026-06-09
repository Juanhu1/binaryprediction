using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BinaryPrediction.Infrastructure.Services;

public class QueueMonitoringService : IQueueMonitoringService
{
    private readonly BinaryPredictionDbContext _dbContext;

    public QueueMonitoringService(BinaryPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<QueueStatusDto> GetQueueStatusAsync(CancellationToken cancellationToken = default)
    {
        var groups = await _dbContext.MarketAnalysisQueueItems
            .AsNoTracking()
            .GroupBy(q => q.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status, g => g.Count, cancellationToken);

        return new QueueStatusDto
        {
            PendingAnalyses = groups.TryGetValue(BinaryPrediction.Core.Enums.AnalysisQueueStatus.Pending, out var pending) ? pending : 0,
            ProcessingAnalyses = groups.TryGetValue(BinaryPrediction.Core.Enums.AnalysisQueueStatus.Processing, out var processing) ? processing : 0,
            CompletedAnalyses = groups.TryGetValue(BinaryPrediction.Core.Enums.AnalysisQueueStatus.Completed, out var completed) ? completed : 0,
            FailedAnalyses = groups.TryGetValue(BinaryPrediction.Core.Enums.AnalysisQueueStatus.Failed, out var failed) ? failed : 0
        };
    }
}
