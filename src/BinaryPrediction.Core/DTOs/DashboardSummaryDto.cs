namespace BinaryPrediction.Core.DTOs;

public class DashboardSummaryDto
{
    public int TotalMarkets { get; set; }
    public int ResolvedMarkets { get; set; }
    public int TotalPredictions { get; set; }
    public int EvaluatedPredictions { get; set; }
    public decimal AccuracyPercentage { get; set; }
    public decimal AverageConfidencePercentage { get; set; }
    public decimal AverageBrierScore { get; set; }
    public ConfidenceBandPerformanceDto? BestConfidenceBand { get; set; }
    public ConfidenceBandPerformanceDto? WorstConfidenceBand { get; set; }
}
