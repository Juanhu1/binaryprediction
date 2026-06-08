using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BinaryPrediction.Api.Controllers;

[ApiController]
[Route("api/performance")]
public class PredictionPerformanceController : ControllerBase
{
    private readonly IPredictionDashboardService _dashboardService;
    private readonly ILogger<PredictionPerformanceController> _logger;

    public PredictionPerformanceController(
        IPredictionDashboardService dashboardService,
        ILogger<PredictionPerformanceController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await _dashboardService.GetSummaryAsync(cancellationToken);
        return Ok(summary);
    }

    [HttpGet("trends/daily")]
    [ProducesResponseType(typeof(List<AccuracyTrendDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDailyTrends(CancellationToken cancellationToken)
    {
        var trends = await _dashboardService.GetDailyAccuracyTrendAsync(cancellationToken);
        return Ok(trends);
    }

    [HttpGet("trends/weekly")]
    [ProducesResponseType(typeof(List<AccuracyTrendDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWeeklyTrends(CancellationToken cancellationToken)
    {
        var trends = await _dashboardService.GetWeeklyAccuracyTrendAsync(cancellationToken);
        return Ok(trends);
    }

    [HttpGet("trends/monthly")]
    [ProducesResponseType(typeof(List<AccuracyTrendDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMonthlyTrends(CancellationToken cancellationToken)
    {
        var trends = await _dashboardService.GetMonthlyAccuracyTrendAsync(cancellationToken);
        return Ok(trends);
    }

    [HttpGet("confidence-bands")]
    [ProducesResponseType(typeof(List<ConfidenceBandPerformanceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConfidenceBands(CancellationToken cancellationToken)
    {
        var bands = await _dashboardService.GetConfidenceBandsAsync(cancellationToken);
        return Ok(bands);
    }

    [HttpGet("benchmarks")]
    [ProducesResponseType(typeof(BenchmarkComparisonDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBenchmarks(CancellationToken cancellationToken)
    {
        var benchmarks = await _dashboardService.GetBenchmarksAsync(cancellationToken);
        return Ok(benchmarks);
    }
}
