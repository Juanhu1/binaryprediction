using BinaryPrediction.Core.Enums;
using System.Text.Json.Serialization;

namespace BinaryPrediction.Api.Models;

public class EligibleMarketResponse
{
    [JsonPropertyName("question")]
    public string Question { get; set; } = string.Empty;

    [JsonPropertyName("qualityScore")]
    public int QualityScore { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("probability")]
    public decimal Probability { get; set; }

    [JsonPropertyName("endDate")]
    public DateTimeOffset? EndDate { get; set; }

    [JsonPropertyName("estimatedResolutionDateUtc")]
    public DateTimeOffset? EstimatedResolutionDateUtc { get; set; }

    [JsonPropertyName("liquidity")]
    public decimal Liquidity { get; set; }

    [JsonPropertyName("volume")]
    public decimal Volume { get; set; }
}
