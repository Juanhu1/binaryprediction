namespace BinaryPrediction.Infrastructure.External.Kalshi;

public class KalshiSettings
{
    public string BaseUrl { get; set; } = "https://external-api.kalshi.com/trade-api/v2";
    public bool Enabled { get; set; } = true;
    public int PageSize { get; set; } = 100;
    public int MaxPages { get; set; } = 5;
    public int TimeoutSeconds { get; set; } = 30;
}
