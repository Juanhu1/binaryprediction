namespace BinaryPrediction.Core.DTOs;

public class PredictionAccuracySummaryDto
{
    public int TotalEvaluatedPredictions { get; set; }
    public int CorrectPredictions { get; set; }
    public int IncorrectPredictions { get; set; }
    public decimal AccuracyPercentage { get; set; }
    public decimal? AverageConfidence { get; set; }
    public decimal? AverageBrierScore { get; set; }
}
