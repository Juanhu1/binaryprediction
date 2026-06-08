namespace BinaryPrediction.Core.DTOs;

public class AccuracyTrendDto
{
    public string Date { get; set; } = string.Empty;
    public int PredictionCount { get; set; }
    public decimal AccuracyPercentage { get; set; }
    public decimal AverageBrierScore { get; set; }
}
