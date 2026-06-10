using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BinaryPrediction.Api.Controllers
{
    [ApiController]
    [Route("api/analytics")]
    public class PredictionAnalyticsController : ControllerBase
    {
        private readonly BinaryPredictionDbContext _dbContext;

        public PredictionAnalyticsController(BinaryPredictionDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // GET /api/analytics/performance
        [HttpGet("performance")]
        public async Task<ActionResult<OverallPerformanceDto>> GetOverallPerformance()
        {
            var snapshot = await _dbContext.PredictionPerformanceSnapshots
                .OrderByDescending(s => s.SnapshotDateUtc)
                .FirstOrDefaultAsync();

            if (snapshot == null) return NotFound();

            return new OverallPerformanceDto
            {
                AccuracyPercentage = snapshot.AccuracyPercentage,
                AverageBrierScore = snapshot.AverageBrierScore,
                TotalPredictions = snapshot.TotalPredictions
            };
        }

        // GET /api/analytics/performance/history
        [HttpGet("performance/history")]
        public async Task<ActionResult<IEnumerable<PerformanceHistoryDto>>> GetPerformanceHistory()
        {
            var history = await _dbContext.PredictionPerformanceSnapshots
                .OrderBy(s => s.SnapshotDateUtc)
                .Select(s => new PerformanceHistoryDto
                {
                    Date = s.SnapshotDateUtc.ToDateTime(TimeOnly.MinValue),
                    AccuracyPercentage = s.AccuracyPercentage,
                    AverageBrierScore = s.AverageBrierScore
                })
                .ToListAsync();

            return history;
        }

        // GET /api/analytics/categories
        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<CategoryTrendDto>>> GetCategoryTrends()
        {
            var latestDate = await _dbContext.CategoryPerformanceSnapshots
                .MaxAsync(s => (DateOnly?)s.SnapshotDateUtc);
            if (latestDate == null) return NotFound();

            var snapshots = await _dbContext.CategoryPerformanceSnapshots
                .Where(s => s.SnapshotDateUtc == latestDate)
                .Include(s => s.PredictionCategory)
                .Select(s => new CategoryTrendDto
                {
                    Category = s.PredictionCategory != null ? s.PredictionCategory.Name : "Unknown",
                    AccuracyPercentage = s.AccuracyPercentage,
                    PredictionCount = s.PredictionCount
                })
                .ToListAsync();

            return snapshots;
        }

        // GET /api/analytics/calibration
        [HttpGet("calibration")]
        public async Task<ActionResult<IEnumerable<CalibrationTrendDto>>> GetCalibrationTrends()
        {
            var latestDate = await _dbContext.CalibrationTrendSnapshots
                .MaxAsync(s => (DateOnly?)s.SnapshotDateUtc);
            if (latestDate == null) return NotFound();

            var snapshots = await _dbContext.CalibrationTrendSnapshots
                .Where(s => s.SnapshotDateUtc == latestDate)
                .Select(s => new CalibrationTrendDto
                {
                    ConfidenceRange = s.ConfidenceRange,
                    ActualAccuracyPercentage = s.ActualAccuracyPercentage,
                    ExpectedAccuracyPercentage = s.ExpectedAccuracyPercentage,
                    CalibrationError = s.CalibrationError
                })
                .ToListAsync();

            return snapshots;
        }
    }

    // DTOs
    public class OverallPerformanceDto
    {
        public decimal AccuracyPercentage { get; set; }
        public decimal AverageBrierScore { get; set; }
        public int TotalPredictions { get; set; }
    }

    public class PerformanceHistoryDto
    {
        public DateTime Date { get; set; }
        public decimal AccuracyPercentage { get; set; }
        public decimal AverageBrierScore { get; set; }
    }

    public class CategoryTrendDto
    {
        public string Category { get; set; } = string.Empty;
        public decimal AccuracyPercentage { get; set; }
        public int PredictionCount { get; set; }
    }

    public class CalibrationTrendDto
    {
        public string ConfidenceRange { get; set; } = string.Empty;
        public decimal ActualAccuracyPercentage { get; set; }
        public decimal ExpectedAccuracyPercentage { get; set; }
        public decimal CalibrationError { get; set; }
    }
}
