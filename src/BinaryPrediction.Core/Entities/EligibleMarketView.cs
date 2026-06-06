namespace BinaryPrediction.Core.Entities;

public class EligibleMarketView
{
    public Guid Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public Enums.MarketCategory Category { get; set; }
    public int? QualityScore { get; set; }
    public decimal Probability { get; set; }
    public decimal Liquidity { get; set; }
    public decimal Volume { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public DateTimeOffset? EstimatedResolutionDateUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
