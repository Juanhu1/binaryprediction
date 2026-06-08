using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BinaryPrediction.Api.Controllers;

[ApiController]
[Route("api/predictions/quality")]
public class PredictionQualityController : ControllerBase
{
    private readonly IPredictionQualityService _qualityService;
    private readonly IConfidenceBandService _confidenceBandService;
    private readonly IMarketCategoryPerformanceService _categoryPerformanceService;
    private readonly IPredictionsImprovementService _improvementService;

    public PredictionQualityController(
        IPredictionQualityService qualityService,
        IConfidenceBandService confidenceBandService,
        IMarketCategoryPerformanceService categoryPerformanceService,
        IPredictionsImprovementService improvementService)
    {
        _qualityService = qualityService;
        _confidenceBandService = confidenceBandService;
        _categoryPerformanceService = categoryPerformanceService;
        _improvementService = improvementService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PredictionQualityReport), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQualityReport(CancellationToken cancellationToken)
    {
        var report = await _qualityService.GenerateAsync(cancellationToken);
        return Ok(report);
    }

    [HttpGet("confidence-bands")]
    [ProducesResponseType(typeof(List<ConfidenceBandPerformanceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConfidenceBands(CancellationToken cancellationToken)
    {
        var bands = await _confidenceBandService.GetConfidenceBandPerformanceAsync(cancellationToken);
        return Ok(bands);
    }

    [HttpGet("categories")]
    [ProducesResponseType(typeof(List<MarketCategoryPerformanceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var categories = await _categoryPerformanceService.GetCategoryPerformanceAsync(cancellationToken);
        return Ok(categories);
    }

    [HttpGet("recommendations")]
    [ProducesResponseType(typeof(List<ImprovementRecommendation>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecommendations(CancellationToken cancellationToken)
    {
        var recommendations = await _improvementService.GenerateRecommendationsAsync(cancellationToken);
        return Ok(recommendations);
    }
}
