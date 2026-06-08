namespace BinaryPrediction.Core.DTOs;

public class ConfidenceBandPerformanceDto
{
    public string BandName { get; set; } = string.Empty;
    public int PredictionCount { get; set; }
    public int CorrectCount { get; set; }
    public decimal AccuracyPercentage { get; set; }
    public decimal AverageBrierScore { get; set; }
}
