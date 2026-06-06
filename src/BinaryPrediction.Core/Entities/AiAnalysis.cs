using BinaryPrediction.Core.Entities.Common;

namespace BinaryPrediction.Core.Entities;

public class AiAnalysis : BaseEntity
{
    public Guid MarketId { get; set; }

    public decimal MarketProbability { get; set; }

    public decimal EstimatedProbability { get; set; }

    public decimal Edge { get; set; }

    public decimal Confidence { get; set; }

    public string Summary { get; set; } = string.Empty;

    public string KeyReasonsJson { get; set; } = string.Empty;

    public string RiskFactorsJson { get; set; } = string.Empty;

    public string ModelName { get; set; } = string.Empty;

    public string PromptVersion { get; set; } = string.Empty;

}
