using BinaryPrediction.Api.Models;
using BinaryPrediction.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BinaryPrediction.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MarketsController : ControllerBase
{
    private readonly IMarketQueryService _marketQueryService;
    private readonly ILogger<MarketsController> _logger;

    public MarketsController(IMarketQueryService marketQueryService, ILogger<MarketsController> logger)
    {
        _marketQueryService = marketQueryService;
        _logger = logger;
    }

    [HttpGet("eligible")]
    public async Task<IActionResult> GetEligibleMarkets([FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Eligible markets endpoint requested. Returning {Count} markets.", limit);
        
        var markets = await _marketQueryService.GetEligibleMarketsAsync(limit, cancellationToken);
        
        var response = markets
            .Select(m => new EligibleMarketResponse
            {
                Question = m.Question,
                QualityScore = m.QualityScore ?? 0,
                Category = m.Category.ToString(),
                Probability = m.Probability,
                EndDate = m.EndDate,
                EstimatedResolutionDateUtc = m.EstimatedResolutionDateUtc,
                Liquidity = m.Liquidity,
                Volume = m.Volume
            })
            .ToList();

        return Ok(response);
    }
}
