namespace BinaryPrediction.Core.DTOs;

public class AiPredictionResultDto
{
    public string PredictedOutcome { get; set; } = string.Empty;
    public decimal ConfidencePercentage { get; set; }
    public string ReasoningSummary { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}
