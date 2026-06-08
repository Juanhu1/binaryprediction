namespace BinaryPrediction.Core.Common;

public static class PredictionConfidenceBand
{
    public static string GetBand(decimal confidencePercentage)
    {
        return confidencePercentage switch
        {
            >= 90m and <= 100m => "90-100",
            >= 80m and < 90m => "80-89",
            >= 70m and < 80m => "70-79",
            >= 60m and < 70m => "60-69",
            >= 50m and < 60m => "50-59",
            _ => "Unknown"
        };
    }
}
