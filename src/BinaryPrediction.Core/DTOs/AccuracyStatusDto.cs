namespace BinaryPrediction.Core.DTOs;

public class AccuracyStatusDto
{
    public decimal AccuracyPercentage { get; set; }
    public decimal AverageBrierScore { get; set; }
    public int PredictionsEvaluated { get; set; }
    public int CorrectPredictions { get; set; }
    public int IncorrectPredictions { get; set; }
}
