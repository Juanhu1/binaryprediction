namespace BinaryPrediction.Core.Common;

public class WorkerSettings
{
    public int MarketCollectionMinutes { get; set; } = 60;
    public int QueueCreationMinutes { get; set; } = 15;
    public int MaintenanceMinutes { get; set; } = 60;
}
