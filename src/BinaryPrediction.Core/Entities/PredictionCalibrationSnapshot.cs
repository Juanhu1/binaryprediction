using System;

namespace BinaryPrediction.Core.Entities;

public class PredictionCalibrationSnapshot
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string ConfidenceRange { get; set; } = string.Empty; // e.g., "50-59"
    public int PredictionCount { get; set; }
    public decimal ActualAccuracyPercentage { get; set; }
    public decimal ExpectedAccuracyPercentage { get; set; }
    public decimal CalibrationError { get; set; }
}
