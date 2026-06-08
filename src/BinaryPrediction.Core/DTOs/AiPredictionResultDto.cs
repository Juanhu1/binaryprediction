namespace BinaryPrediction.Core.DTOs;

public class AiPredictionResultDto
{
    public string PredictedOutcome { get; set; } = string.Empty;
    public decimal ConfidenceScore { get; set; }
    public string ReasoningSummary { get; set; } = string.Empty;
}
