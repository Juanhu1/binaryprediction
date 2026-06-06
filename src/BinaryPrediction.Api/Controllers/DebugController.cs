using BinaryPrediction.Core.Entities;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BinaryPrediction.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DebugController : ControllerBase
{
    private readonly BinaryPredictionDbContext _dbContext;

    public DebugController(BinaryPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("eligible-markets")]
    public async Task<IActionResult> GetEligibleMarkets(CancellationToken cancellationToken)
    {
        var markets = await _dbContext.Set<Market>()
            .Where(m => m.EligibleForAnalysis)
            .OrderByDescending(m => m.QualityScore)
            .Select(m => new { m.Id, m.Slug, m.Question, m.Category, m.QualityScore })
            .Take(50)
            .ToListAsync(cancellationToken);

        return Ok(markets);
    }

    [HttpGet("rejected-markets")]
    public async Task<IActionResult> GetRejectedMarkets(CancellationToken cancellationToken)
    {
        var markets = await _dbContext.Set<Market>()
            .Where(m => !m.EligibleForAnalysis && m.RejectionReason != null)
            .OrderByDescending(m => m.LastQualityEvaluationUtc)
            .Select(m => new { m.Id, m.Slug, m.Question, m.Category, m.QualityScore, m.RejectionReason })
            .Take(50)
            .ToListAsync(cancellationToken);

        return Ok(markets);
    }

    [HttpGet("market-quality/{marketId}")]
    public async Task<IActionResult> GetMarketQuality(Guid marketId, CancellationToken cancellationToken)
    {
        var market = await _dbContext.Set<Market>()
            .Where(m => m.Id == marketId)
            .Select(m => new 
            { 
                m.Id, 
                m.Slug, 
                m.Question, 
                m.Category, 
                m.QualityScore, 
                m.EligibleForAnalysis, 
                m.RejectionReason, 
                m.LastQualityEvaluationUtc,
                m.Liquidity,
                m.Volume
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (market == null)
            return NotFound();

        return Ok(market);
    }
}
