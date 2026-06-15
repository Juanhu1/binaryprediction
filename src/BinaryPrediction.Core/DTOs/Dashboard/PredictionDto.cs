namespace BinaryPrediction.Core.DTOs.Dashboard
{
    public class PredictionDto
    {
        public Guid Id { get; set; }
        public Guid MarketId { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string PredictedOutcome { get; set; } = string.Empty;
        public decimal ConfidencePercentage { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public DateTimeOffset? EvaluatedDate { get; set; }
        public string? ActualOutcome { get; set; }
        public decimal? PredictionError { get; set; }
    }
}
