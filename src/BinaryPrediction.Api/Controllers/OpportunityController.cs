using Microsoft.AspNetCore.Mvc;
using BinaryPrediction.Core.Interfaces;
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
    private readonly IPredictionOpportunityRepository _repo;
    private readonly IEdgeDetectionService _edgeDetectionService;

    public OpportunityController(IPredictionOpportunityRepository repo, IEdgeDetectionService edgeDetectionService)
    {
        _repo = repo;
        _edgeDetectionService = edgeDetectionService;
    }

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

    // Temporary diagnostic endpoint for Day 16
    [HttpPost("test/{predictionId:guid}")]
    public async Task<ActionResult<object>> TestEdgeDetection([FromRoute] Guid predictionId, CancellationToken cancellationToken)
    {
        await _edgeDetectionService.DetectOpportunityAsync(predictionId, cancellationToken);
        return Ok(new
        {
            success = true,
            predictionId = predictionId,
            message = "Edge detection executed."
        });
    }
}

