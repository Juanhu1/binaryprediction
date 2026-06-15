namespace BinaryPrediction.Core.DTOs.Dashboard
{
    public class SystemDto
    {
        public int TotalMarkets { get; set; }
        public int MarketsAddedToday { get; set; }
        public int TotalPredictions { get; set; }
        public int PredictionsGeneratedToday { get; set; }
        public int PredictionsEvaluatedToday { get; set; }
        public int TotalOpportunities { get; set; }
        public int OpportunitiesDetectedToday { get; set; }
        public DateTimeOffset? LatestAnalyticsSnapshotDate { get; set; }
    }
}
