using BinaryPrediction.Core.Enums;

namespace BinaryPrediction.Core.Common;

public class MarketFilteringSettings
{
    public decimal MinimumLiquidity { get; set; } = 1000m;
    public decimal MinimumVolume { get; set; } = 1000m;
    public decimal MinimumQualityScore { get; set; } = 50m;
    public List<MarketCategory> EligibleCategories { get; set; } = new() 
    { 
        MarketCategory.Politics, 
        MarketCategory.Sports, 
        MarketCategory.Crypto, 
        MarketCategory.Economics, 
        MarketCategory.Technology, 
        MarketCategory.Entertainment 
    };
    public int MaximumMarketDurationDays { get; set; } = 365;
    public int ReanalysisCooldownMinutes { get; set; } = 1440;
    public int MaxMarketsPerCycle { get; set; } = 10;
}
