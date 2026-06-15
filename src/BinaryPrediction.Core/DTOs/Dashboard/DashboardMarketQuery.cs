namespace BinaryPrediction.Core.DTOs.Dashboard
{
    public class DashboardMarketQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
        public string? Category { get; set; }
        public string? Status { get; set; }
        public string? SortBy { get; set; }
        public bool SortDesc { get; set; } = false;
    }
}
