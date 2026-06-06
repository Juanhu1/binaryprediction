namespace BinaryPrediction.Core.Common;

public class AnalysisSettings
{
    public int AnalysisIntervalMinutes { get; set; } = 5;
    public decimal MinimumLiquidity { get; set; } = 1000m;
    public decimal MinimumVolume { get; set; } = 1000m;
    public int ReanalysisCooldownMinutes { get; set; } = 1440;
    public decimal MinimumConfidence { get; set; } = 50m;
}
