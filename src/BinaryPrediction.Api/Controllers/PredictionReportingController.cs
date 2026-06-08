using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BinaryPrediction.Api.Controllers;

[ApiController]
[Route("api/predictions")]
public class PredictionReportingController : ControllerBase
{
    private readonly IPredictionStatisticsService _statisticsService;

    public PredictionReportingController(IPredictionStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    [HttpGet("accuracy")]
    [ProducesResponseType(typeof(PredictionAccuracySummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAccuracyAsync(CancellationToken cancellationToken)
    {
        var summary = await _statisticsService.GetAccuracySummaryAsync(cancellationToken);
        return Ok(summary);
    }

    [HttpGet("confidence-calibration")]
    [ProducesResponseType(typeof(IReadOnlyList<ConfidenceBucketAccuracyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConfidenceCalibrationAsync(CancellationToken cancellationToken)
    {
        var buckets = await _statisticsService.GetConfidenceBucketAccuracyAsync(cancellationToken);
        return Ok(buckets);
    }
}
