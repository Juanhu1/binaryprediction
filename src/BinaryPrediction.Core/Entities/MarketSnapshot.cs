using BinaryPrediction.Core.Entities.Common;

namespace BinaryPrediction.Core.Entities;

public class MarketSnapshot : BaseEntity
{
    public Guid MarketId { get; set; }

    public decimal Probability { get; set; }

    public decimal Liquidity { get; set; }
}
