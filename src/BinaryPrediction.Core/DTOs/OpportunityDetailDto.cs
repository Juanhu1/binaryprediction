using System;
using System.Collections.Generic;
using BinaryPrediction.Core.Enums;
using BinaryPrediction.Core.Entities;

namespace BinaryPrediction.Core.DTOs
{
    /// <summary>
    /// Detailed view of a PredictionOpportunity used by the admin API.
    /// </summary>
    public class OpportunityDetailDto
    {
        public Guid Id { get; set; }
        public Guid PredictionId { get; set; }
        public Guid MarketId { get; set; }
        public decimal MarketProbability { get; set; }
        public decimal AiProbability { get; set; }
        public decimal ProbabilityGap { get; set; }
        public GapDirection GapDirection { get; set; }
        public bool HasEdge { get; set; }
        public DateTimeOffset DetectedAtUtc { get; set; }
        public OpportunityStatus Status { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset LastStatusChangedAtUtc { get; set; }
        public DateTimeOffset? IgnoredAtUtc { get; set; }
        public DateTimeOffset? ExpiredAtUtc { get; set; }
        public DateTimeOffset? ResolvedAtUtc { get; set; }
        public IEnumerable<OpportunityStatusHistoryDto> StatusHistory { get; set; } = new List<OpportunityStatusHistoryDto>();
    }

    public class OpportunityStatusHistoryDto
    {
        public Guid Id { get; set; }
        public OpportunityStatus PreviousStatus { get; set; }
        public OpportunityStatus NewStatus { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTimeOffset ChangedAtUtc { get; set; }
    }
}
