using BinaryPrediction.Core.Enums;
using System.Text.Json.Serialization;

namespace BinaryPrediction.Api.Models;

public class AnalysisQueueResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("marketId")]
    public Guid MarketId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    [JsonPropertyName("retryCount")]
    public int RetryCount { get; set; }

    [JsonPropertyName("createdAtUtc")]
    public DateTimeOffset CreatedAtUtc { get; set; }

    [JsonPropertyName("startedAtUtc")]
    public DateTimeOffset? StartedAtUtc { get; set; }

    [JsonPropertyName("completedAtUtc")]
    public DateTimeOffset? CompletedAtUtc { get; set; }

    [JsonPropertyName("lastError")]
    public string? LastError { get; set; }
}
