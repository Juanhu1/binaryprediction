using Microsoft.AspNetCore.Mvc;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Core.DTOs.Dashboard;
using Microsoft.Extensions.Logging;

namespace BinaryPrediction.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<DashboardOverviewDto>> GetOverview([FromQuery] CancellationToken ct)
    {
        _logger.LogInformation("Dashboard overview requested");
        var result = await _dashboardService.GetOverviewAsync(ct);
        return Ok(result);
    }

    [HttpGet("markets")]
    public async Task<ActionResult<PaginatedResult<MarketDto>>> GetMarkets([FromQuery] DashboardMarketQuery query, CancellationToken ct)
    {
        _logger.LogInformation("Dashboard markets requested: Page {Page}, Size {Size}", query.Page, query.PageSize);
        var result = await _dashboardService.GetMarketsAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("predictions")]
    public async Task<ActionResult<PaginatedResult<PredictionDto>>> GetPredictions([FromQuery] DashboardPredictionQuery query, CancellationToken ct)
    {
        _logger.LogInformation("Dashboard predictions requested: Page {Page}, Size {Size}", query.Page, query.PageSize);
        var result = await _dashboardService.GetPredictionsAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("prediction/{id:guid}")]
    public async Task<ActionResult<PredictionDetailsDto>> GetPredictionDetails(Guid id, CancellationToken ct)
    {
        _logger.LogInformation("Fetching prediction details for {Id}", id);
        var result = await _dashboardService.GetPredictionDetailsAsync(id, ct);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("opportunities")]
    public async Task<ActionResult<OpportunityQueryResult>> GetOpportunities([FromQuery] DashboardOpportunityQuery query, CancellationToken ct)
    {
        _logger.LogInformation("Dashboard opportunities requested: Page {Page}, Size {Size}", query.Page, query.PageSize);
        var result = await _dashboardService.GetOpportunitiesAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("analytics")]
    public async Task<ActionResult<AnalyticsDto>> GetAnalytics(CancellationToken ct)
    {
        _logger.LogInformation("Dashboard analytics requested");
        var result = await _dashboardService.GetAnalyticsAsync(ct);
        return Ok(result);
    }

    [HttpGet("system")]
    public async Task<ActionResult<SystemDto>> GetSystem(CancellationToken ct)
    {
        _logger.LogInformation("Dashboard system stats requested");
        var result = await _dashboardService.GetSystemAsync(ct);
        return Ok(result);
    }
}
