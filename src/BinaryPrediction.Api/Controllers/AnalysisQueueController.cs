using BinaryPrediction.Api.Models;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Enums;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BinaryPrediction.Api.Controllers;

[ApiController]
[Route("api/analysis-queue")]
public class AnalysisQueueController : ControllerBase
{
    private readonly BinaryPredictionDbContext _dbContext;

    public AnalysisQueueController(BinaryPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetQueueItems(
        [FromQuery] AnalysisQueueStatus? status = null, 
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        IQueryable<MarketAnalysisQueueItem> query = _dbContext.MarketAnalysisQueueItems.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(q => q.Status == status.Value);
        }

        var items = await query
            .OrderByDescending(q => q.Priority)
            .ThenByDescending(q => q.CreatedAtUtc)
            .Take(limit)
            .Select(q => new AnalysisQueueResponse
            {
                Id = q.Id,
                MarketId = q.MarketId,
                Status = q.Status.ToString(),
                Priority = q.Priority,
                RetryCount = q.RetryCount,
                CreatedAtUtc = q.CreatedAtUtc,
                StartedAtUtc = q.StartedAtUtc,
                CompletedAtUtc = q.CompletedAtUtc,
                LastError = q.LastError
            })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }
}
