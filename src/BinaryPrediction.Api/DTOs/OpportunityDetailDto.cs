using System;
using BinaryPrediction.Core.Enums;

namespace BinaryPrediction.Api.DTOs;

/// <summary>
/// Detailed data transfer object for a prediction opportunity, used when fetching by status.
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
}
