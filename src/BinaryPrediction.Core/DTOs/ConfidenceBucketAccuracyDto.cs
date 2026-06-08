namespace BinaryPrediction.Core.DTOs;

public class ConfidenceBucketAccuracyDto
{
    public string Bucket { get; set; } = string.Empty;
    public int TotalPredictions { get; set; }
    public int CorrectPredictions { get; set; }
    public decimal AccuracyPercentage { get; set; }
    public decimal AverageConfidence { get; set; }
    public decimal? AverageBrierScore { get; set; }
}
