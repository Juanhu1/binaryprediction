using System;

namespace BinaryPrediction.Api.DTOs;

/// <summary>
/// Opportunity information for the admin dashboard.
/// </summary>
public class OpportunityAdminDto
{
    public Guid PredictionId { get; set; }
    public string MarketTitle { get; set; } = string.Empty;
    public decimal MarketProbability { get; set; }
    public decimal AiProbability { get; set; }
    public decimal ProbabilityGap { get; set; }
    public string GapDirection { get; set; } = string.Empty; // "AIHigher" or "AILower"
    public bool HasEdge { get; set; }
    public DateTimeOffset DetectedAtUtc { get; set; }
}
