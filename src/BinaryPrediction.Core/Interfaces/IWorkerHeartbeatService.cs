namespace BinaryPrediction.Core.Interfaces;

public interface IWorkerHeartbeatService
{
    Task LogHeartbeatAsync(string workerName, string status = "Healthy", string? errorMessage = null, CancellationToken cancellationToken = default);
}
