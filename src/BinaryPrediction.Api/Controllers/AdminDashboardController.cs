using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Services;
using BinaryPrediction.Core.Interfaces;

namespace BinaryPrediction.Api.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly IAdminDashboardService _adminService;
        private readonly ISystemHealthService _systemHealthService;

        public AdminDashboardController(IAdminDashboardService adminService, ISystemHealthService systemHealthService)
        {
            _adminService = adminService;
            _systemHealthService = systemHealthService;
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
    }
}
