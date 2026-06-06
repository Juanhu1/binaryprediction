using BinaryPrediction.Core.Enums;

namespace BinaryPrediction.Core.Common;

public class AnalysisRefreshSettings
{
    public int Politics { get; set; } = 24;
    public int Crypto { get; set; } = 6;
    public int Sports { get; set; } = 6;
    public int Other { get; set; } = 12;

    public int GetRefreshHoursForCategory(MarketCategory category)
    {
        return category switch
        {
            MarketCategory.Politics => Politics,
            MarketCategory.Crypto => Crypto,
            MarketCategory.Sports => Sports,
            _ => Other
        };
    }
}
