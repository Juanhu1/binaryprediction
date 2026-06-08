using BinaryPrediction.Core.Entities.Common;

namespace BinaryPrediction.Core.Entities;

public class PredictionQualitySnapshot : BaseEntity
{
    public DateTimeOffset SnapshotDateUtc { get; set; }
    public int TotalPredictions { get; set; }
    public double AccuracyPercentage { get; set; }
    public double AverageBrierScore { get; set; }
    public double CalibrationError { get; set; }
    public double BenchmarkAdvantage { get; set; }
    public double ImprovementTrend { get; set; }
}
