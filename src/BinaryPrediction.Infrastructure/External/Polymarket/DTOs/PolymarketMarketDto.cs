using BinaryPrediction.Infrastructure.External.Polymarket.Serialization;
using System.Text.Json.Serialization;

namespace BinaryPrediction.Infrastructure.External.Polymarket.DTOs;

public class PolymarketMarketDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("question")]
    public string? Question { get; set; }

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("active")]
    public bool? Active { get; set; }

    [JsonPropertyName("closed")]
    public bool? Closed { get; set; }

    [JsonPropertyName("liquidity")]
    public decimal? Liquidity { get; set; }

    [JsonPropertyName("volume")]
    public decimal? Volume { get; set; }

    [JsonPropertyName("outcomes")]
    [JsonConverter(typeof(JsonStringArrayConverter))]
    public IReadOnlyList<string>? Outcomes { get; set; }

    [JsonPropertyName("outcomePrices")]
    [JsonConverter(typeof(JsonStringArrayConverter))]
    public IReadOnlyList<string>? OutcomePrices { get; set; }

    [JsonPropertyName("tags")]
    [JsonConverter(typeof(JsonStringArrayConverter))]
    public IReadOnlyList<string>? Tags { get; set; }

    [JsonPropertyName("endDate")]
    public DateTimeOffset? EndDate { get; set; }

    [JsonPropertyName("closeDate")]
    public DateTimeOffset? CloseDate { get; set; }

    [JsonPropertyName("eventDate")]
    public DateTimeOffset? EventDate { get; set; }

    [JsonPropertyName("resolveDate")]
    public DateTimeOffset? ResolveDate { get; set; }

    [JsonPropertyName("gameDate")]
    public DateTimeOffset? GameDate { get; set; }

    [JsonPropertyName("tournamentDate")]
    public DateTimeOffset? TournamentDate { get; set; }
}
