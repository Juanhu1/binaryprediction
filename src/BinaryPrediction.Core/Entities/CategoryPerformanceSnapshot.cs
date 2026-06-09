using System;

namespace BinaryPrediction.Core.Entities;

public class CategoryPerformanceSnapshot
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string Category { get; set; } = string.Empty;
    public int PredictionCount { get; set; }
    public decimal AccuracyPercentage { get; set; }
    public decimal AverageBrierScore { get; set; }
    public decimal AverageConfidence { get; set; }
}
