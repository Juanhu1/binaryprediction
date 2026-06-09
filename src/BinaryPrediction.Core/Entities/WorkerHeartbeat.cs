namespace BinaryPrediction.Core.Entities;

public class WorkerHeartbeat
{
    public string WorkerName { get; set; } = string.Empty;
    public DateTimeOffset LastHeartbeatUtc { get; set; }
    public string Status { get; set; } = "Healthy";
    public string? LastErrorMessage { get; set; }
}
