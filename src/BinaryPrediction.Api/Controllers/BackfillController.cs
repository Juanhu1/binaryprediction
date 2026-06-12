using Microsoft.AspNetCore.Mvc;
using BinaryPrediction.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace BinaryPrediction.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BackfillController : ControllerBase
{
    private readonly IPredictionBackfillService _backfillService;
    private readonly ILogger<BackfillController> _logger;

    public BackfillController(IPredictionBackfillService backfillService, ILogger<BackfillController> logger)
    {
        _backfillService = backfillService;
        _logger = logger;
    }

    [HttpPost("predictions")]
    public async Task<IActionResult> BackfillPredictions([FromQuery] int batchSize = 1000)
    {
        var updated = await _backfillService.BackfillAsync(batchSize);
        _logger.LogInformation("Backfill completed, {Count} records updated.", updated);
        return Ok(new { UpdatedRecords = updated });
    }
}
