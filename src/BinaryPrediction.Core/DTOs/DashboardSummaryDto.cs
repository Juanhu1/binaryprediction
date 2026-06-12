namespace BinaryPrediction.Core.DTOs;

public class DashboardSummaryDto
{
    public int TotalMarkets { get; set; }
    public int ActiveMarkets { get; set; }
    public int ClosedMarkets { get; set; }
    public int TotalAnalyses { get; set; }
    public int TotalPredictions { get; set; }
    public int ResolvedPredictions { get; set; }
    public int PendingEvaluationPredictions { get; set; }
    public decimal AccuracyPercentage { get; set; }
    public decimal AverageBrierScore { get; set; }
    public int TotalOpportunities { get; set; }
    public int ActiveOpportunities { get; set; }
    public DateTimeOffset? LastMarketPullUtc { get; set; }
    public DateTimeOffset? LastAnalysisUtc { get; set; }
    public DateTimeOffset? LastPredictionUtc { get; set; }
    // New properties required by PredictionDashboardService
    public int ResolvedMarkets { get; set; }
    public int EvaluatedPredictions { get; set; }
    public decimal AverageConfidencePercentage { get; set; }
    public ConfidenceBandPerformanceDto? BestConfidenceBand { get; set; }
    public ConfidenceBandPerformanceDto? WorstConfidenceBand { get; set; }
}
