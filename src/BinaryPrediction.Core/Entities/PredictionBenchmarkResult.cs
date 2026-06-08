using BinaryPrediction.Core.Entities.Common;

namespace BinaryPrediction.Core.Entities;

public class PredictionBenchmarkResult : BaseEntity
{
    public string BenchmarkType { get; set; } = string.Empty;
    public int TotalPredictions { get; set; }
    public int CorrectPredictions { get; set; }
    public decimal AccuracyPercentage { get; set; }
    public decimal AverageBrierScore { get; set; }
    public DateTimeOffset CalculatedAtUtc { get; set; }
}
