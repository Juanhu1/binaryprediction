namespace BinaryPrediction.Core.DTOs;

public class PredictionAccuracySummary
{
    public int TotalPredictions { get; set; }
    public int ResolvedPredictions { get; set; }
    public int CorrectPredictions { get; set; }
    public decimal AccuracyPercentage { get; set; }
    public decimal AverageBrierScore { get; set; }
}
