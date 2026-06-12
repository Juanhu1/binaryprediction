using System;
using System.ComponentModel.DataAnnotations;

namespace BinaryPrediction.Core.Entities
{
    public class PredictionPerformanceSnapshot
    {
        [Key]
        public Guid Id { get; set; }

        public DateOnly SnapshotDateUtc { get; set; }
        public int TotalPredictions { get; set; }
        public int CorrectPredictions { get; set; }
        public int IncorrectPredictions { get; set; }
        public decimal AccuracyPercentage { get; set; }
        public decimal AverageConfidence { get; set; }
        public decimal AverageBrierScore { get; set; }
        public decimal AveragePredictionError { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
    }
}
