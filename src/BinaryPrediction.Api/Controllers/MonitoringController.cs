using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BinaryPrediction.Api.Controllers;

[ApiController]
[Route("api/monitoring")]
public class MonitoringController : ControllerBase
{
    private readonly ISystemHealthService _systemHealthService;
    private readonly IQueueMonitoringService _queueMonitoringService;
    private readonly IOpenAiMonitoringService _openAiMonitoringService;

    public MonitoringController(
        ISystemHealthService systemHealthService,
        IQueueMonitoringService queueMonitoringService,
        IOpenAiMonitoringService openAiMonitoringService)
    {
        _systemHealthService = systemHealthService;
        _queueMonitoringService = queueMonitoringService;
        _openAiMonitoringService = openAiMonitoringService;
    }

    [HttpGet("health")]
    [ProducesResponseType(typeof(SystemHealthDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemHealthDto>> GetHealth(CancellationToken cancellationToken)
    {
        var result = await _systemHealthService.GetCurrentHealthAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("queues")]
    [ProducesResponseType(typeof(QueueStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<QueueStatusDto>> GetQueues(CancellationToken cancellationToken)
    {
        var result = await _queueMonitoringService.GetQueueStatusAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("usage")]
    [ProducesResponseType(typeof(OpenAiStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OpenAiStatusDto>> GetUsage(CancellationToken cancellationToken)
    {
        var result = await _openAiMonitoringService.GetOpenAiStatusAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("snapshots")]
    [ProducesResponseType(typeof(IReadOnlyList<SystemHealthSnapshot>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SystemHealthSnapshot>>> GetSnapshots(
        [FromQuery] int limit = 24, 
        CancellationToken cancellationToken = default)
    {
        var result = await _systemHealthService.GetHistoricalSnapshotsAsync(limit, cancellationToken);
        Console.WriteLine($"DB returned {result.Count} snapshots");
        return Ok(result);
    }
}
