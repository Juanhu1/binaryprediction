namespace BinaryPrediction.Core.DTOs.Dashboard
{
    public class MarketDto
    {
        public Guid Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public DateTimeOffset CreatedDate { get; set; }
        public DateTimeOffset? ResolutionDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
