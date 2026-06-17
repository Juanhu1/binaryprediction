namespace BinaryPrediction.Core.DTOs.Dashboard
{
    public class DashboardOpportunityQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Status { get; set; }
        public decimal? MinGap { get; set; }
        public decimal? MaxGap { get; set; }
        public string? SortBy { get; set; }
        public bool SortDesc { get; set; } = false;
        public string? Search { get; set; }
        public string? Source { get; set; }
    }
}
