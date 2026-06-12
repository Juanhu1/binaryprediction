using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;

namespace BinaryPrediction.Api.Controllers
{
    [ApiController]
    [Route("api/resolution")]
    public class PredictionResolutionController : ControllerBase
    {
        private readonly IPredictionResolutionService _resolutionService;
        private readonly IPredictionResolutionRepository _repository;

        public PredictionResolutionController(IPredictionResolutionService resolutionService,
                                             IPredictionResolutionRepository repository)
        {
            _resolutionService = resolutionService;
            _repository = repository;
        }

        // GET /api/resolution/summary
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var pending = await _repository.GetPendingCountAsync();
            var resolved = await _repository.GetResolvedCountAsync();
            var resolvedToday = await _repository.GetResolvedTodayAsync();

            var result = new
            {
                pending,
                resolved,
                resolvedToday
            };
            return Ok(result);
        }

        // POST /api/resolution/run
        [HttpPost("run")]
        public async Task<IActionResult> RunEvaluation()
        {
            var processed = await _resolutionService.ProcessPendingPredictionsAsync();
            return Ok(new { processed });
        }

        // GET /api/resolution/pending
        [HttpGet("pending")]
        public async Task<ActionResult<List<Prediction>>> GetPending()
        {
            var pending = await _repository.GetPendingPredictionsAsync();
            return Ok(pending);
        }
    }
}
