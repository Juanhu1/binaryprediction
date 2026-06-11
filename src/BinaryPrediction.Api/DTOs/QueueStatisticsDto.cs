using System;

namespace BinaryPrediction.Api.DTOs;

/// <summary>
/// Queue statistics for the admin dashboard.
/// </summary>
public class QueueStatisticsDto
{
    public int PendingMarketAnalyses { get; set; }
    public int CompletedMarketAnalyses { get; set; }
    public int PendingPredictions { get; set; }
    public int CompletedPredictions { get; set; }
    public int FailedAnalyses { get; set; }
    public int FailedPredictions { get; set; }
}
