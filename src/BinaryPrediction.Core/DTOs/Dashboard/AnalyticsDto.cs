using System.Collections.Generic;

namespace BinaryPrediction.Core.DTOs.Dashboard
{
    public class AnalyticsDto
    {
        public decimal AccuracyPercentage { get; set; }
        public decimal AverageConfidence { get; set; }
        public decimal AveragePredictionError { get; set; }
        public List<ConfidenceBucketDto> ConfidenceBuckets { get; set; } = new();
        public List<CalibrationSnapshotDto> CalibrationSnapshots { get; set; } = new();

        // New evaluation summary
        public int TotalEvaluated { get; set; }
        public int CorrectCount { get; set; }
        public int IncorrectCount { get; set; }
    }
}
