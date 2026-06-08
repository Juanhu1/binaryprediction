using BinaryPrediction.Core.Entities.Common;

namespace BinaryPrediction.Core.Entities;

public class Market : BaseEntity
{
    public string Question { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public bool Active { get; set; }

    public bool Closed { get; set; }

    public decimal Liquidity { get; set; }

    public decimal Volume { get; set; }

    public decimal Probability { get; set; }

    public DateTimeOffset? EndDate { get; set; }

    public DateTimeOffset? EstimatedResolutionDateUtc { get; set; }

    public int? QualityScore { get; set; }

    public BinaryPrediction.Core.Enums.MarketCategory Category { get; set; }

    public bool EligibleForAnalysis { get; set; }

    public DateTimeOffset? LastQualityEvaluationUtc { get; set; }

    public string? RejectionReason { get; set; }

    public string? ActualOutcome { get; set; }

    public DateTimeOffset? ResolvedAtUtc { get; set; }
}
