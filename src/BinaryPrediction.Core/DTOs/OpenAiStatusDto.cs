namespace BinaryPrediction.Core.DTOs;

public class OpenAiStatusDto
{
    public int CallsToday { get; set; }
    public int SuccessfulCallsToday { get; set; }
    public int FailedCallsToday { get; set; }
    public int TotalTokensToday { get; set; }
    public decimal TotalCostTodayUsd { get; set; }
    public double AverageLatencyMs { get; set; }
    public string? MostRecentError { get; set; }
    public DateTimeOffset? MostRecentErrorUtc { get; set; }
    
    public bool HasAlert => FailedCallsToday > 0 && ((double)FailedCallsToday / CallsToday) > 0.1;
}
