using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BinaryPrediction.Infrastructure.Services;

public class WorkerHeartbeatService : IWorkerHeartbeatService
{
    private readonly BinaryPredictionDbContext _dbContext;
    private readonly ILogger<WorkerHeartbeatService> _logger;

    public WorkerHeartbeatService(BinaryPredictionDbContext dbContext, ILogger<WorkerHeartbeatService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task LogHeartbeatAsync(string workerName, string status = "Healthy", string? errorMessage = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var heartbeat = await _dbContext.WorkerHeartbeats
                .FirstOrDefaultAsync(h => h.WorkerName == workerName, cancellationToken);

            if (heartbeat == null)
            {
                heartbeat = new WorkerHeartbeat
                {
                    WorkerName = workerName,
                    LastHeartbeatUtc = DateTimeOffset.UtcNow,
                    Status = status,
                    LastErrorMessage = errorMessage
                };
                _dbContext.WorkerHeartbeats.Add(heartbeat);
            }
            else
            {
                heartbeat.LastHeartbeatUtc = DateTimeOffset.UtcNow;
                heartbeat.Status = status;
                heartbeat.LastErrorMessage = errorMessage;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Do not fail the worker if heartbeat fails
            _logger.LogWarning(ex, "Failed to log heartbeat for {WorkerName}", workerName);
        }
    }
}
