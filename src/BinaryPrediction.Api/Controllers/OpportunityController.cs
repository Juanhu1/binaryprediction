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

    public OpportunityController(IPredictionOpportunityRepository repo)
    {
        _repo = repo;
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
}
