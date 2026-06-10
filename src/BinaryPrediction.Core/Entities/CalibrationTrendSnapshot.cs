using System;
using System.ComponentModel.DataAnnotations;

namespace BinaryPrediction.Core.Entities
{
    public class CalibrationTrendSnapshot
    {
        [Key]
        public Guid Id { get; set; }

        public string ConfidenceRange { get; set; } = string.Empty;
        public DateOnly SnapshotDateUtc { get; set; }
        public int PredictionCount { get; set; }
        public decimal ActualAccuracyPercentage { get; set; }
        public decimal ExpectedAccuracyPercentage { get; set; }
        public decimal CalibrationError { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
    }
}
