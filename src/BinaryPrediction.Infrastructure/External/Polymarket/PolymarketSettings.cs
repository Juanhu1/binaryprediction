namespace BinaryPrediction.Infrastructure.External.Polymarket;

public class PolymarketSettings
{
    public const string SectionName = "Polymarket";

    public string BaseUrl { get; set; } = "https://gamma-api.polymarket.com";

    public int PollingIntervalMinutes { get; set; } = 5;

    public int TimeoutSeconds { get; set; } = 30;

    public int PageSize { get; set; } = 100;

    public int MaxPages { get; set; } = 1;
}
