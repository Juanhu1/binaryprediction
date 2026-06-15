using System.Threading;
using System.Threading.Tasks;
using BinaryPrediction.Core.DTOs.Dashboard;

namespace BinaryPrediction.Core.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardOverviewDto> GetOverviewAsync(CancellationToken ct = default);
        Task<PaginatedResult<MarketDto>> GetMarketsAsync(DashboardMarketQuery query, CancellationToken ct = default);
        Task<PaginatedResult<PredictionDto>> GetPredictionsAsync(DashboardPredictionQuery query, CancellationToken ct = default);
        Task<PaginatedResult<OpportunityDto>> GetOpportunitiesAsync(DashboardOpportunityQuery query, CancellationToken ct = default);
        Task<AnalyticsDto> GetAnalyticsAsync(CancellationToken ct = default);
        Task<SystemDto> GetSystemAsync(CancellationToken ct = default);
        Task<PredictionDetailsDto?> GetPredictionDetailsAsync(Guid predictionId, CancellationToken ct = default);
    }
}
