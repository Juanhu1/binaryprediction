namespace BinaryPrediction.Core.DTOs;

public class AiPerformanceDto
{
    public int AnalysesGenerated { get; set; }
    public int PredictionsGenerated { get; set; }
    public int FailedAnalyses { get; set; }
    public int FailedPredictions { get; set; }
    public double AverageLatencyMs { get; set; }
    public decimal AverageCostUsd { get; set; }
}
