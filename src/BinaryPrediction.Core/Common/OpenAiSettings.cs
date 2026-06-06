namespace BinaryPrediction.Core.Common;

public class OpenAiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4-turbo-preview";
    public decimal Temperature { get; set; } = 0.2m;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxAnalysesPerMinute { get; set; } = 5;
    public int DailyAnalysisLimit { get; set; } = 500;
}
