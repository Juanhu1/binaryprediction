using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Services;
using BinaryPrediction.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace BinaryPrediction.Infrastructure.Services
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly IAdminDashboardRepository _repository;
        private readonly ILogger<AdminDashboardService> _logger;

        public AdminDashboardService(IAdminDashboardRepository repository, ILogger<AdminDashboardService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching dashboard summary");
            return _repository.GetDashboardSummaryAsync(cancellationToken);
        }

        public Task<(IReadOnlyList<MarketAdminDto> Items, int Total)> GetMarketsAsync(int page, int pageSize, string? status, string? search, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching markets page {Page} size {PageSize} status {Status} search {Search}", page, pageSize, status, search);
            return _repository.GetMarketsAsync(page, pageSize, status, search, cancellationToken);
        }

        public Task<(IReadOnlyList<PredictionAdminDto> Items, int Total)> GetPredictionsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching predictions page {Page} size {PageSize}", page, pageSize);
            return _repository.GetPredictionsAsync(page, pageSize, cancellationToken);
        }

        public Task<(IReadOnlyList<OpportunityAdminDto> Items, int Total)> GetOpportunitiesAsync(bool? hasEdge, decimal? minGap, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching opportunities page {Page} size {PageSize} hasEdge {HasEdge} minGap {MinGap}", page, pageSize, hasEdge, minGap);
            return _repository.GetOpportunitiesAsync(hasEdge, minGap, page, pageSize, cancellationToken);
        }

        public Task<QueueStatisticsDto> GetQueueStatisticsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching queue statistics");
            return _repository.GetQueueStatisticsAsync(cancellationToken);
        }
    }
}
