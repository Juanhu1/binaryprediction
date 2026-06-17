using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace BinaryPrediction.Infrastructure.External.Kalshi;

public interface IKalshiClient
{
    Task<IReadOnlyList<KalshiMarketDto>> GetActiveMarketsAsync(CancellationToken cancellationToken);
}

public class KalshiMarketsResponse
{
    [JsonPropertyName("markets")]
    public List<KalshiMarketDto> Markets { get; set; } = new();

    [JsonPropertyName("cursor")]
    public string? Cursor { get; set; }
}

public class KalshiMarketDto
{
    [JsonPropertyName("ticker")]
    public string? Ticker { get; set; }

    [JsonPropertyName("event_ticker")]
    public string? EventTicker { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("close_time")]
    public DateTimeOffset? CloseTime { get; set; }

    [JsonPropertyName("expiration_time")]
    public DateTimeOffset? ExpirationTime { get; set; }

    [JsonPropertyName("expected_expiration_time")]
    public DateTimeOffset? ExpectedExpirationTime { get; set; }

    [JsonPropertyName("volume_fp")]
    public decimal? VolumeFp { get; set; }

    [JsonPropertyName("liquidity_dollars")]
    public decimal? LiquidityDollars { get; set; }

    [JsonPropertyName("yes_bid_dollars")]
    public decimal? YesBidDollars { get; set; }

    [JsonPropertyName("yes_ask_dollars")]
    public decimal? YesAskDollars { get; set; }

    [JsonPropertyName("last_price_dollars")]
    public decimal? LastPriceDollars { get; set; }
}
