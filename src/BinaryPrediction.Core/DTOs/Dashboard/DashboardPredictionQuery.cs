namespace BinaryPrediction.Core.DTOs.Dashboard
{
    public class DashboardPredictionQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
        public bool PendingOnly { get; set; } = false;
        public bool EvaluatedOnly { get; set; } = false;
        public string? Category { get; set; }
        public decimal? ConfidenceMin { get; set; }
        public decimal? ConfidenceMax { get; set; }
        public string? SortBy { get; set; }
        public bool SortDesc { get; set; } = false;
    }
}
