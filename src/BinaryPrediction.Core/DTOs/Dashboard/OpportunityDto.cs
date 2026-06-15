namespace BinaryPrediction.Core.DTOs.Dashboard
{
    public class OpportunityDto
    {
        public Guid Id { get; set; }
        public Guid PredictionId { get; set; }
        public Guid MarketId { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string MarketSlug { get; set; } = string.Empty;
        public decimal MarketProbability { get; set; }
        public decimal AiProbability { get; set; }
        public decimal ProbabilityGap { get; set; }
        public string Direction { get; set; } = string.Empty;
        public bool HasEdge { get; set; }
        public decimal ConfidencePercentage { get; set; }
        public decimal EdgeScore { get; set; }
        public DateTimeOffset DetectedAtUtc { get; set; }
        public string PolymarketUrl { get; set; } = string.Empty;
        public DateTimeOffset? EndDate { get; set; }
    }
}
