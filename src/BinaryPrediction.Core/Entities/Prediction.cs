using BinaryPrediction.Core.Entities.Common;

namespace BinaryPrediction.Core.Entities;

public class Prediction : BaseEntity
{
    public Guid MarketId { get; set; }
    public Guid AnalysisId { get; set; }
    public string PredictedOutcome { get; set; } = string.Empty;
    public decimal ConfidencePercentage { get; set; }
    public string ReasoningSummary { get; set; } = string.Empty;
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public bool IsActive { get; set; }
    public string? ActualOutcome { get; set; }
    public bool? WasCorrect { get; set; }
    public DateTimeOffset? EvaluatedAtUtc { get; set; }
    public decimal? BrierScore { get; set; }
    public decimal? PredictionError { get; set; }
    // Added property for AI probability used in admin dashboards
    public decimal AiProbability { get; set; }
    // New resolution fields
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public decimal? ActualOutcomeProbability { get; set; }
    public string? ResolutionSource { get; set; }

    public string PromptVersionUsed { get; set; } = string.Empty;

    public Market? Market { get; set; }
}
