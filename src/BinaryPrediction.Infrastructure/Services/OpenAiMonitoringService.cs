using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BinaryPrediction.Infrastructure.Services;

public class OpenAiMonitoringService : IOpenAiMonitoringService
{
    private readonly BinaryPredictionDbContext _dbContext;

    public OpenAiMonitoringService(BinaryPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OpenAiStatusDto> GetOpenAiStatusAsync(CancellationToken cancellationToken = default)
    {
        var todayUtc = new DateTimeOffset(DateTimeOffset.UtcNow.UtcDateTime.Date, TimeSpan.Zero);

        var recordsToday = await _dbContext.AiUsageRecords
            .Where(r => r.CreatedAtUtc >= todayUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var calls = recordsToday.Count;
        var successes = recordsToday.Count(r => r.IsSuccess);
        var failures = calls - successes;
        var totalTokens = recordsToday.Sum(r => r.TotalTokens);
        var totalCost = recordsToday.Sum(r => r.EstimatedCostUsd);
        var avgLatency = calls > 0 ? recordsToday.Average(r => r.LatencyMs) : 0;

        var mostRecentError = await _dbContext.AiUsageRecords
            .Where(r => !r.IsSuccess)
            .OrderByDescending(r => r.CreatedAtUtc)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        return new OpenAiStatusDto
        {
            CallsToday = calls,
            SuccessfulCallsToday = successes,
            FailedCallsToday = failures,
            TotalTokensToday = totalTokens,
            TotalCostTodayUsd = totalCost,
            AverageLatencyMs = Math.Round(avgLatency, 2),
            MostRecentError = mostRecentError?.ErrorMessage,
            MostRecentErrorUtc = mostRecentError?.CreatedAtUtc
        };
    }
}
