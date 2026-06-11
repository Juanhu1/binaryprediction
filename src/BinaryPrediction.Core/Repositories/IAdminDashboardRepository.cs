using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BinaryPrediction.Core.DTOs;

namespace BinaryPrediction.Core.Repositories
{
    public interface IAdminDashboardRepository
    {
        Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);
        Task<(IReadOnlyList<MarketAdminDto> Items, int Total)> GetMarketsAsync(int page, int pageSize, string? status, string? search, CancellationToken cancellationToken = default);
        Task<(IReadOnlyList<PredictionAdminDto> Items, int Total)> GetPredictionsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
        Task<(IReadOnlyList<OpportunityAdminDto> Items, int Total)> GetOpportunitiesAsync(bool? hasEdge, decimal? minGap, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<QueueStatisticsDto> GetQueueStatisticsAsync(CancellationToken cancellationToken = default);
    }
}
