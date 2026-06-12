using BinaryPrediction.Core.Entities.Common;
using System;

namespace BinaryPrediction.Core.Entities
{
    /// <summary>
    /// Permanent audit trail for prediction resolution outcomes.
    /// </summary>
    public class PredictionResolutionHistory : BaseEntity
    {
        /// <summary>
        /// Foreign key to the Prediction that was resolved.
        /// </summary>
        public Guid PredictionId { get; set; }

        /// <summary>
        /// Foreign key to the Market containing the prediction.
        /// </summary>
        public Guid MarketId { get; set; }

        /// <summary>
        /// Confidence percentage of the original prediction.
        /// </summary>
        public decimal ConfidencePercentage { get; set; }

        /// <summary>
        /// The actual outcome of the market (e.g., "Yes" or "No").
        /// </summary>
        public string? ActualOutcome { get; set; }

        /// <summary>
        /// Indicates whether the prediction was correct.
        /// </summary>
        public bool? WasCorrect { get; set; }

        /// <summary>
        /// Calculated Brier score for the prediction.
        /// </summary>
        public decimal? BrierScore { get; set; }

        /// <summary>
        /// Timestamp when the market resolved.
        /// </summary>
        public DateTimeOffset? ResolvedAtUtc { get; set; }
    }
}
