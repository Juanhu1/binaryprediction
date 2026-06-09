using BinaryPrediction.Core.Entities.Common;

namespace BinaryPrediction.Core.Entities;

public class AiUsageRecord : BaseEntity
{
    public Guid? MarketId { get; set; }
    
    // e.g., "Analysis" or "Prediction"
    public string OperationType { get; set; } = string.Empty;
    
    public string Model { get; set; } = string.Empty;
    
    public int PromptTokens { get; set; }
    
    public int CompletionTokens { get; set; }
    
    public int TotalTokens { get; set; }
    
    public decimal EstimatedCostUsd { get; set; }
    
    public bool IsSuccess { get; set; } = true;
    
    public long LatencyMs { get; set; }
    
    public string? ErrorMessage { get; set; }
}
