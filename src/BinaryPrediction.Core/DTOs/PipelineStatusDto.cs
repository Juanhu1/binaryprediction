namespace BinaryPrediction.Core.DTOs;

public class PipelineStatusDto
{
    public int TotalMarkets { get; set; }
    public int TotalAnalyses { get; set; }
    public int TotalPredictions { get; set; }
    public int AnalysesWithoutPredictions { get; set; }
    public int ResolvedMarkets { get; set; }
    public int EvaluatedPredictions { get; set; }

    public bool HasAlert => AnalysesWithoutPredictions > 0;
}
