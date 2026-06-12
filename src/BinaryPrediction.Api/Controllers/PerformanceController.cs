using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BinaryPrediction.Api.Controllers;

[ApiController]
[Route("api/performance")]
public class PerformanceController : ControllerBase
{
    private readonly IPredictionPerformanceService _performanceService;
    private readonly ILogger<PerformanceController> _logger;

    public PerformanceController(IPredictionPerformanceService performanceService, ILogger<PerformanceController> logger)
    {
        _performanceService = performanceService;
        _logger = logger;
    }

    [HttpPost("generate-snapshot")]
    public async Task<IActionResult> GenerateSnapshot(  )
    {
        await _performanceService.GenerateDailySnapshotAsync(DateTime.UtcNow );

        return Ok();
    }


    // GET /api/performance/current
    [HttpGet("current")]
    [ProducesResponseType(typeof(CurrentPerformanceDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentPerformance(CancellationToken ct)
    {
        var snapshot = await _performanceService.GetCurrentPerformanceAsync();
        if (snapshot == null)
            return NotFound();
        var dto = new CurrentPerformanceDto
        {
            TotalPredictions = snapshot.TotalPredictions,
            CorrectPredictions = snapshot.CorrectPredictions,
            AccuracyPercentage = snapshot.AccuracyPercentage,
            AverageConfidence = snapshot.AverageConfidence,
            AverageBrierScore = snapshot.AverageBrierScore,
            AveragePredictionError = snapshot.AveragePredictionError
        };
        return Ok(dto);
    }

    // GET /api/performance/history
    [HttpGet("history")]
    [ProducesResponseType(typeof(IEnumerable<PredictionPerformanceSnapshotDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory([FromQuery] DateTime? startDateUtc, [FromQuery] DateTime? endDateUtc, CancellationToken ct)
    {
        // If dates omitted, return all snapshots
        var start = startDateUtc?.Date ?? DateTime.MinValue;
        var end = endDateUtc?.Date ?? DateTime.MaxValue;
        var snapshots = await _performanceService.GetPerformanceTrendAsync(start, end);
        var dtos = snapshots.Select(s => new PredictionPerformanceSnapshotDto
        {
            SnapshotDateUtc = s.SnapshotDateUtc,
            TotalPredictions = s.TotalPredictions,
            CorrectPredictions = s.CorrectPredictions,
            IncorrectPredictions = s.IncorrectPredictions,
            AccuracyPercentage = s.AccuracyPercentage,
            AverageConfidence = s.AverageConfidence,
            AverageBrierScore = s.AverageBrierScore,
            AveragePredictionError = s.AveragePredictionError
        });
        return Ok(dtos);
    }

    // GET /api/performance/trend
    [HttpGet("trend")]
    [ProducesResponseType(typeof(PerformanceTrendDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTrend(CancellationToken ct)
    {
        var now = DateTime.UtcNow.Date;
        var last7 = await _performanceService.GetPerformanceTrendAsync(now.AddDays(-6), now);
        var last30 = await _performanceService.GetPerformanceTrendAsync(now.AddDays(-29), now);
        var all = await _performanceService.GetPerformanceTrendAsync(DateTime.MinValue, now);
        var dto = new PerformanceTrendDto
        {
            Last7Days = last7.Select(MapDto).ToList(),
            Last30Days = last30.Select(MapDto).ToList(),
            AllTime = all.Select(MapDto).ToList()
        };
        return Ok(dto);
    }

    private static PredictionPerformanceSnapshotDto MapDto(PredictionPerformanceSnapshot s) => new()
    {
        SnapshotDateUtc = s.SnapshotDateUtc,
        TotalPredictions = s.TotalPredictions,
        CorrectPredictions = s.CorrectPredictions,
        IncorrectPredictions = s.IncorrectPredictions,
        AccuracyPercentage = s.AccuracyPercentage,
        AverageConfidence = s.AverageConfidence,
        AverageBrierScore = s.AverageBrierScore,
        AveragePredictionError = s.AveragePredictionError
    };
}

public class CurrentPerformanceDto
{
    public int TotalPredictions { get; set; }
    public int CorrectPredictions { get; set; }
    public decimal AccuracyPercentage { get; set; }
    public decimal AverageConfidence { get; set; }
    public decimal AverageBrierScore { get; set; }
    public decimal AveragePredictionError { get; set; }
}

public class PredictionPerformanceSnapshotDto
{
    public DateOnly SnapshotDateUtc { get; set; }
    public int TotalPredictions { get; set; }
    public int CorrectPredictions { get; set; }
    public int IncorrectPredictions { get; set; }
    public decimal AccuracyPercentage { get; set; }
    public decimal AverageConfidence { get; set; }
    public decimal AverageBrierScore { get; set; }
    public decimal AveragePredictionError { get; set; }
}

public class PerformanceTrendDto
{
    public List<PredictionPerformanceSnapshotDto> Last7Days { get; set; } = new();
    public List<PredictionPerformanceSnapshotDto> Last30Days { get; set; } = new();
    public List<PredictionPerformanceSnapshotDto> AllTime { get; set; } = new();
}
