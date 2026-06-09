using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BinaryPrediction.Api.Controllers;

[ApiController]
[Route("api/ai/performance")]
public class AiPerformanceController : ControllerBase
{
    private readonly IAiPerformanceService _performanceService;

    public AiPerformanceController(IAiPerformanceService performanceService)
    {
        _performanceService = performanceService;
    }

    [HttpGet]
    public async Task<ActionResult<AiPerformanceDto>> GetPerformanceAsync(CancellationToken cancellationToken)
    {
        var result = await _performanceService.GetPerformanceAsync(cancellationToken);
        return Ok(result);
    }
}
