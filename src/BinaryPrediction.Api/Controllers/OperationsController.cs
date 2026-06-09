using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BinaryPrediction.Api.Controllers;

[ApiController]
[Route("api/system")]
public class OperationsController : ControllerBase
{
    private readonly IHealthMonitoringService _healthMonitoringService;
    private readonly IQueueMonitoringService _queueMonitoringService;
    private readonly IPipelineMonitoringService _pipelineMonitoringService;
    private readonly IOpenAiMonitoringService _openAiMonitoringService;
    private readonly IAccuracyMonitoringService _accuracyMonitoringService;

    public OperationsController(
        IHealthMonitoringService healthMonitoringService,
        IQueueMonitoringService queueMonitoringService,
        IPipelineMonitoringService pipelineMonitoringService,
        IOpenAiMonitoringService openAiMonitoringService,
        IAccuracyMonitoringService accuracyMonitoringService)
    {
        _healthMonitoringService = healthMonitoringService;
        _queueMonitoringService = queueMonitoringService;
        _pipelineMonitoringService = pipelineMonitoringService;
        _openAiMonitoringService = openAiMonitoringService;
        _accuracyMonitoringService = accuracyMonitoringService;
    }

    [HttpGet("health")]
    [ProducesResponseType(typeof(SystemHealthDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemHealthDto>> GetHealth(CancellationToken cancellationToken)
    {
        var result = await _healthMonitoringService.GetSystemHealthAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("queue")]
    [ProducesResponseType(typeof(QueueStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<QueueStatusDto>> GetQueue(CancellationToken cancellationToken)
    {
        var result = await _queueMonitoringService.GetQueueStatusAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("pipeline")]
    [ProducesResponseType(typeof(PipelineStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PipelineStatusDto>> GetPipeline(CancellationToken cancellationToken)
    {
        var result = await _pipelineMonitoringService.GetPipelineStatusAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("openai")]
    [ProducesResponseType(typeof(OpenAiStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OpenAiStatusDto>> GetOpenAiStatus(CancellationToken cancellationToken)
    {
        var result = await _openAiMonitoringService.GetOpenAiStatusAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("accuracy")]
    [ProducesResponseType(typeof(AccuracyStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AccuracyStatusDto>> GetAccuracyStatus(CancellationToken cancellationToken)
    {
        var result = await _accuracyMonitoringService.GetAccuracyStatusAsync(cancellationToken);
        return Ok(result);
    }
}
