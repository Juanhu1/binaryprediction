using BinaryPrediction.Core.Entities.Common;
using BinaryPrediction.Core.Enums;

namespace BinaryPrediction.Core.Entities;

public class MarketAnalysisQueueItem : BaseEntity
{
    public Guid MarketId { get; set; }
    public Market? Market { get; set; }
    
    public AnalysisQueueStatus Status { get; set; }
    public int Priority { get; set; }
    public int RetryCount { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    
    public string? LastError { get; set; }

    public MarketAnalysisQueueItem()
    {
        CreatedAtUtc = DateTimeOffset.UtcNow;
        Priority = 0;
        RetryCount = 0;
        Status = AnalysisQueueStatus.Pending;
    }
}
