namespace BinaryPrediction.Core.DTOs;

public class BenchmarkResultDto
{
    public string BenchmarkType { get; set; } = string.Empty;
    public int TotalPredictions { get; set; }
    public int CorrectPredictions { get; set; }
    public decimal AccuracyPercentage { get; set; }
    public decimal AverageBrierScore { get; set; }
}
