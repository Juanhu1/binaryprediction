using BinaryPrediction.Core.Entities.Common;
using BinaryPrediction.Core.Enums;

namespace BinaryPrediction.Core.Entities;

public class SystemHealthSnapshot : BaseEntity
{
    public HealthStatus Status { get; set; }

    public int TotalMarkets { get; set; }
    public int ActiveMarkets { get; set; }
    public int ResolvedMarkets { get; set; }

    public int QueuedMarkets { get; set; }
    public int ProcessingMarkets { get; set; }
    public int CompletedMarkets { get; set; }
    public int FailedMarkets { get; set; }

    public int TotalAnalyses { get; set; }
    public int TotalPredictions { get; set; }

    public decimal PredictionAccuracy { get; set; }
    public decimal AverageBrierScore { get; set; }

    public DateTimeOffset? LastMarketCollectionUtc { get; set; }
    public DateTimeOffset? LastAnalysisUtc { get; set; }
    public DateTimeOffset? LastPredictionUtc { get; set; }
    public DateTimeOffset? LastEvaluationUtc { get; set; }
}
