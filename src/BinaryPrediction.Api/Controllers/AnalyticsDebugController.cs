using Microsoft.AspNetCore.Mvc;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BinaryPrediction.Api.Controllers
{
    [ApiController]
    [Route("api/analytics/debug")]
    public class AnalyticsDebugController : ControllerBase
    {
        private readonly BinaryPredictionDbContext _db;
        public AnalyticsDebugController(BinaryPredictionDbContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<object>> GetDebugInfo()
        {
            var predictionCount = await _db.Predictions.CountAsync();
            var evaluatedCount = await _db.Predictions.CountAsync(p => p.ActualOutcome != null);
            var correctCount = await _db.Predictions.CountAsync(p => p.ActualOutcome != null && p.ActualOutcome == p.PredictedOutcome);
            var performanceSnapshots = await _db.PredictionPerformanceSnapshots.CountAsync();
            var categorySnapshots = await _db.CategoryPerformanceSnapshots.CountAsync();
            var calibrationSnapshots = await _db.CalibrationTrendSnapshots.CountAsync();

            return Ok(new
            {
                predictionCount,
                evaluatedCount,
                correctCount,
                performanceSnapshots,
                categorySnapshots,
                calibrationSnapshots
            });
        }
    }
}
