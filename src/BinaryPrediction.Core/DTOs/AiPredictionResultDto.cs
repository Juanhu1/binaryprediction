using System.Text.Json.Serialization;

namespace BinaryPrediction.Core.DTOs;

public class AiPredictionResultDto
{
    [JsonPropertyName("predictedOutcome")]
    public string PredictedOutcome { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public decimal ConfidencePercentage { get; set; }

    [JsonPropertyName("reasoning")]
    public string ReasoningSummary { get; set; } = string.Empty;

    [JsonPropertyName("eventProbability")]
    public decimal EventProbability { get; set; }

    public string PromptVersionUsed { get; set; } = "v2";

    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}
