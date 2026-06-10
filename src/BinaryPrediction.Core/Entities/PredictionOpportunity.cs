using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BinaryPrediction.Core.Entities;

/// <summary>
/// Represents an identified edge where the AI prediction probability diverges from the market probability.
/// </summary>
public class PredictionOpportunity
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>Foreign key to the market.</summary>
    public Guid MarketId { get; set; }
    public Market? Market { get; set; }

    /// <summary>Foreign key to the prediction.</summary>
    public Guid PredictionId { get; set; }
    public Prediction? Prediction { get; set; }

    /// <summary>Probability reported by Polymarket (0‑100).</summary>
    public decimal MarketProbability { get; set; }

    /// <summary>AI‑generated probability (0‑100).</summary>
    public decimal AiProbability { get; set; }

    /// <summary>Absolute difference between AI and market probabilities.</summary>
    public decimal ProbabilityGap { get; set; }

    /// <summary>
    /// Direction of the gap: AI_BULLISH when AI > Market, AI_BEARISH when AI < Market.
    /// </summary>
    [MaxLength(20)]
    public string GapDirection { get; set; } = string.Empty;

    /// <summary>Configured threshold that determines whether this is considered an edge.</summary>
    public decimal EdgeThresholdPercentage { get; set; }

    /// <summary>True when ProbabilityGap >= EdgeThresholdPercentage.</summary>
    public bool HasEdge { get; set; }

    public DateTimeOffset DetectedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
