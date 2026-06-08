namespace BinaryPrediction.Core.DTOs;

public class PredictionQualityReport
{
    public double Accuracy { get; set; }
    public double AverageBrierScore { get; set; }
    public double CalibrationError { get; set; }
    public double BenchmarkAdvantage { get; set; }
    public double ImprovementTrend { get; set; }
}
