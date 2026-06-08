namespace BinaryPrediction.Core.DTOs;

public class MarketCategoryPerformanceDto
{
    public string Category { get; set; } = string.Empty;
    public int PredictionCount { get; set; }
    public decimal AccuracyPercentage { get; set; }
    public decimal AverageBrierScore { get; set; }
}
