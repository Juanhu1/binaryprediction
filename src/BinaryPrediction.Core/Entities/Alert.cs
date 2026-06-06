using BinaryPrediction.Core.Entities.Common;

namespace BinaryPrediction.Core.Entities;

public class Alert : BaseEntity
{
    public Guid MarketId { get; set; }

    public string Message { get; set; } = string.Empty;
}
