namespace BinaryPrediction.Core.DTOs;

public class WorkerHealthDto
{
    public string WorkerName { get; set; } = string.Empty;
    public DateTimeOffset LastHeartbeatUtc { get; set; }
    public string Status { get; set; } = "Healthy";
    public string? LastErrorMessage { get; set; }
    public bool IsAlive { get; set; }
}

public class SystemHealthDto
{
    public List<WorkerHealthDto> Workers { get; set; } = new();
    
    public DateTimeOffset? LastMarketSyncUtc { get; set; }
    public DateTimeOffset? LastAnalysisUtc { get; set; }
    public DateTimeOffset? LastPredictionUtc { get; set; }
    public DateTimeOffset? LastEvaluationUtc { get; set; }
}
