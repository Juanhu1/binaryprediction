using Microsoft.AspNetCore.Mvc;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Core.Enums;
using BinaryPrediction.Api.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BinaryPrediction.Api.Controllers;

[ApiController]
[Route("api/v1/opportunities")]
public class OpportunityController : ControllerBase
{
    private readonly IOpportunityLifecycleService _lifecycleService;
    private readonly IPredictionOpportunityRepository _repo;
    private readonly IOpportunityStatusHistoryRepository _historyRepo;
    private readonly IEdgeDetectionService _edgeDetectionService;

    public OpportunityController(
        IOpportunityLifecycleService lifecycleService,
        IPredictionOpportunityRepository repo,
        IOpportunityStatusHistoryRepository historyRepo,
        IEdgeDetectionService edgeDetectionService)
    {
        _lifecycleService = lifecycleService;
        _repo = repo;
        _historyRepo = historyRepo;
        _edgeDetectionService = edgeDetectionService;
    }

    // GET active opportunities (existing behavior)
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OpportunityDto>>> Get(CancellationToken cancellationToken)
    {
        var opportunities = await _repo.GetActiveAsync(cancellationToken);
        var dtos = opportunities.Select(o => new OpportunityDto
        {
            PredictionId = o.PredictionId,
            MarketId = o.MarketId,
            AiProbability = o.AiProbability,
            MarketProbability = o.MarketProbability,
            ProbabilityGap = o.ProbabilityGap,
            HasEdge = o.HasEdge,
            DetectedAtUtc = o.DetectedAtUtc
        }).ToList();
        return Ok(dtos);
    }

    // Activate opportunity (Open -> Active)
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await _lifecycleService.ChangeStatusAsync(id, OpportunityStatus.Active, "Activated via API", cancellationToken);
        return NoContent();
    }

    // Ignore opportunity (Open/Active -> Ignored)
    [HttpPost("{id:guid}/ignore")]
    public async Task<IActionResult> Ignore([FromRoute] Guid id, [FromBody] string? reason, CancellationToken cancellationToken)
    {
        await _lifecycleService.ChangeStatusAsync(id, OpportunityStatus.Ignored, reason ?? "Ignored via API", cancellationToken);
        return NoContent();
    }

    // Get status history for an opportunity
    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<IReadOnlyList<OpportunityStatusHistoryDto>>> GetHistory([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var history = await _historyRepo.GetByOpportunityIdAsync(id, cancellationToken);
        var dtos = history.Select(h => new OpportunityStatusHistoryDto
        {
            Id = h.Id,
            PreviousStatus = h.PreviousStatus,
            NewStatus = h.NewStatus,
            Reason = h.Reason,
            ChangedAtUtc = h.ChangedAtUtc
        }).ToList();
        return Ok(dtos);
    }

    // Get opportunities filtered by status
    [HttpGet("status/{status}")]
    public async Task<ActionResult<IReadOnlyList<OpportunityDetailDto>>> GetByStatus([FromRoute] OpportunityStatus status, CancellationToken cancellationToken)
    {
        var opportunities = await _repo.GetByStatusAsync(status, cancellationToken);
        var dtos = opportunities.Select(o => new OpportunityDetailDto
        {
            Id = o.Id,
            PredictionId = o.PredictionId,
            MarketId = o.MarketId,
            MarketProbability = o.MarketProbability,
            AiProbability = o.AiProbability,
            ProbabilityGap = o.ProbabilityGap,
            GapDirection = o.GapDirection,
            HasEdge = o.HasEdge,
            DetectedAtUtc = o.DetectedAtUtc,
            Status = o.Status,
            CreatedAtUtc = o.CreatedAtUtc,
            LastStatusChangedAtUtc = o.LastStatusChangedAtUtc,
            IgnoredAtUtc = o.IgnoredAtUtc,
            ExpiredAtUtc = o.ExpiredAtUtc,
            ResolvedAtUtc = o.ResolvedAtUtc
        }).ToList();
        return Ok(dtos);
    }

    // Temporary diagnostic endpoint for Day 16 (preserved)
    [HttpPost("test/{predictionId:guid}")]
    public async Task<ActionResult<object>> TestEdgeDetection([FromRoute] Guid predictionId, CancellationToken cancellationToken)
    {
        await _edgeDetectionService.DetectOpportunityAsync(predictionId, cancellationToken);
        return Ok(new { success = true, predictionId, message = "Edge detection executed." });
    }
}
