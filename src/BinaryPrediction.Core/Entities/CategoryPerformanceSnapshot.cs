using System;

using System.ComponentModel.DataAnnotations;

namespace BinaryPrediction.Core.Entities
{
    public class CategoryPerformanceSnapshot
    {
        [Key]
        public Guid Id { get; set; }

        public Guid PredictionCategoryId { get; set; }
        public PredictionCategory? PredictionCategory { get; set; }
        public DateOnly SnapshotDateUtc { get; set; }
        public int PredictionCount { get; set; }
        public int CorrectPredictions { get; set; }
        public decimal AccuracyPercentage { get; set; }
        public decimal AverageBrierScore { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
    }
}
