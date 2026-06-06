namespace BinaryPrediction.Core.Common;

public class QueueProcessingSettings
{
    public int MaxRetries { get; set; } = 3;
    public int BatchSize { get; set; } = 10;
    public int ArchiveAfterDays { get; set; } = 30;
}
