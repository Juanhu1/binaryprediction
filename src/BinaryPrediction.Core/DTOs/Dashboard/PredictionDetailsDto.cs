namespace BinaryPrediction.Core.DTOs.Dashboard
{
    public class PredictionDetailsDto
    {
        public Guid PredictionId { get; set; }
        public Guid MarketId { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string PredictedOutcome { get; set; } = string.Empty;
        public decimal ConfidencePercentage { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public DateTimeOffset? EvaluatedDate { get; set; }
        public string? ActualOutcome { get; set; }
        public decimal? PredictionError { get; set; }
        public bool? WasCorrect { get; set; }
        // Analysis sections
        public string MarketSummary { get; set; } = string.Empty;
        public List<string> SupportingEvidence { get; set; } = new();
        public List<string> ContradictingEvidence { get; set; } = new();
        public List<string> KeyRisks { get; set; } = new();
        public string ConfidenceExplanation { get; set; } = string.Empty;
        public decimal FinalProbability { get; set; }
    }
}
