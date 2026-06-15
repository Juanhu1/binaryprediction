namespace BinaryPrediction.Core.DTOs.Dashboard
{
    public class DashboardOverviewDto
    {
        // Market Metrics
        public int TotalMarkets { get; set; }
        public int OpenMarkets { get; set; }
        public int ResolvedMarkets { get; set; }

        // Prediction Metrics
        public int TotalPredictions { get; set; }
        public int PendingPredictions { get; set; }
        public int EvaluatedPredictions { get; set; }

        // Performance Metrics
        public decimal AccuracyPercentage { get; set; }
        public decimal AverageConfidence { get; set; }
        public decimal AveragePredictionError { get; set; }

        // Opportunity Metrics
        public int TotalOpportunities { get; set; }
        public int ActiveOpportunities { get; set; }
        public int ResolvedOpportunities { get; set; }
    }
}
