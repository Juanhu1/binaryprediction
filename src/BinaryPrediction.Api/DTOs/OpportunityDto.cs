using System;

namespace BinaryPrediction.Api.DTOs;

/// <summary>
/// Data transfer object representing a prediction opportunity (edge).
/// </summary>
public class OpportunityDto
{
    public Guid PredictionId { get; set; }
    public Guid MarketId { get; set; }
    public decimal AiProbability { get; set; }
    public decimal MarketProbability { get; set; }
    public decimal ProbabilityGap { get; set; }
    public bool HasEdge { get; set; }
    public DateTimeOffset DetectedAtUtc { get; set; }
}
