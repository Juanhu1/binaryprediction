using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Services;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Interfaces;

namespace BinaryPrediction.Api.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly IAdminDashboardService _adminService;
        private readonly IDataRepairService _repairService;
        private readonly ISystemHealthService _systemHealthService;
        private readonly IPerformanceSnapshotService _performanceSnapshotService;

        private readonly IMarketSynchronizationService _marketSyncService;
        public AdminDashboardController(IAdminDashboardService adminService, ISystemHealthService systemHealthService, IDataRepairService repairService, IPerformanceSnapshotService performanceSnapshotService, IMarketSynchronizationService marketSyncService)
        {
            _adminService = adminService;
            _systemHealthService = systemHealthService;
            _repairService = repairService;
            _performanceSnapshotService = performanceSnapshotService;
            _marketSyncService = marketSyncService;
        }

        // GET api/v1/admin/dashboard
        [HttpGet("dashboard")]
        public async Task<ActionResult<DashboardSummaryDto>> GetDashboardSummary()
        {
            var result = await _adminService.GetDashboardSummaryAsync();
            return Ok(result);
        }

        // GET api/v1/admin/system
        [HttpGet("system")]
        public async Task<ActionResult<SystemHealthDto>> GetSystemHealth()
        {
            var result = await _systemHealthService.GetCurrentHealthAsync();
            return Ok(result);
        }

        // GET api/v1/admin/markets
        [HttpGet("markets")]
        public async Task<ActionResult> GetMarkets([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
                                                    [FromQuery] string? status = null, [FromQuery] string? search = null)
        {
            var (items, total) = await _adminService.GetMarketsAsync(page, pageSize, status, search);
            return Ok(new { Items = items, Total = total });
        }

        // GET api/v1/admin/predictions
        [HttpGet("predictions")]
        public async Task<ActionResult> GetPredictions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var (items, total) = await _adminService.GetPredictionsAsync(page, pageSize);
            return Ok(new { Items = items, Total = total });
        }

        // GET api/v1/admin/opportunities
        [HttpGet("opportunities")]
        public async Task<ActionResult> GetOpportunities([FromQuery] bool? hasEdge = null,
                                                          [FromQuery] decimal? minGap = null,
                                                          [FromQuery] int page = 1,
                                                          [FromQuery] int pageSize = 20)
        {
            var (items, total) = await _adminService.GetOpportunitiesAsync(hasEdge, minGap, page, pageSize);
            return Ok(new { Items = items, Total = total });
        }

        // GET api/v1/admin/queues
        [HttpGet("queues")]
        public async Task<ActionResult<QueueStatisticsDto>> GetQueueStatistics()
        {
            var result = await _adminService.GetQueueStatisticsAsync();
            return Ok(result);
        }

        // POST api/admin/repair-opportunities
        [HttpPost("repair-opportunities")]
        public async Task<ActionResult> RepairOpportunities()
        {
            await _repairService.RecomputeAllOpportunitiesAsync();
            return Ok(new { Message = "Opportunities repair completed." });
        }

        // POST api/admin/repair-opportunities-scale
        [HttpPost("repair-opportunities-scale")]
        public async Task<ActionResult> RepairOpportunitiesScale()
        {
            await _repairService.RepairOpportunitiesScaleAsync();
            return Ok(new { Message = "Opportunities probability scale repair completed successfully." });
        }

        // POST api/admin/repair-data
        
        

        // POST api/admin/synchronize-markets
        [HttpPost("synchronize-markets")]
        public async Task<ActionResult> SynchronizeMarkets()
        {
            await _marketSyncService.SynchronizeActiveMarketsAsync();
            return Ok(new { Message = "Market synchronization completed successfully." });
        }

        // POST api/admin/rebuild-snapshots
        [AllowAnonymous]
        [HttpPost("rebuild-snapshots")]
        public async Task<ActionResult> RebuildSnapshots()
        {
            await _performanceSnapshotService.RebuildAllSnapshotsAsync();
            return Ok(new { Message = "Analytics snapshots rebuilt successfully." });
        }

        // POST api/admin/rebuild-eligibility
        [HttpPost("rebuild-eligibility")]
        public async Task<ActionResult> RebuildEligibility(CancellationToken cancellationToken)
        {
            var summary = await _repairService.RebuildMarketEligibilityAsync(cancellationToken);
            return Ok(new { Message = "Market eligibility rebuild completed.", Summary = summary });
        }
    }
}
