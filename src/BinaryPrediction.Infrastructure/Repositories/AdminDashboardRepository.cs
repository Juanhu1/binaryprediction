using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Repositories;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BinaryPrediction.Infrastructure.Repositories
{
    public class AdminDashboardRepository : IAdminDashboardRepository
    {
        private readonly BinaryPredictionDbContext _dbContext;

        public AdminDashboardRepository(BinaryPredictionDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
        {
            var resolvedCount = await _dbContext.Predictions.CountAsync(p => p.EvaluatedAtUtc != null, cancellationToken);
            var correctCount = await _dbContext.Predictions.CountAsync(p => p.EvaluatedAtUtc != null && p.WasCorrect == true, cancellationToken);
            var accuracyPercentage = resolvedCount == 0 ? 0m : (decimal)correctCount * 100m / resolvedCount;
            var averageBrierScore = await _dbContext.Predictions
                .Where(p => p.EvaluatedAtUtc != null && p.BrierScore != null)
                .Select(p => (decimal?)p.BrierScore)
                .AverageAsync(cancellationToken) ?? 0m;

            var summary = new DashboardSummaryDto
            {
                TotalMarkets = await _dbContext.Markets.CountAsync(cancellationToken),
                ActiveMarkets = await _dbContext.Markets.CountAsync(m => m.Active, cancellationToken),
                ClosedMarkets = await _dbContext.Markets.CountAsync(m => m.Closed, cancellationToken),
                TotalAnalyses = await _dbContext.AiAnalyses.CountAsync(cancellationToken),
                TotalPredictions = await _dbContext.Predictions.CountAsync(cancellationToken),
                ResolvedPredictions = resolvedCount,
                AccuracyPercentage = accuracyPercentage,
                AverageBrierScore = averageBrierScore,
                TotalOpportunities = await _dbContext.PredictionOpportunities.CountAsync(cancellationToken),
                ActiveOpportunities = await _dbContext.PredictionOpportunities.CountAsync(o => o.HasEdge, cancellationToken),
                LastMarketPullUtc = await _dbContext.MarketSnapshots.MaxAsync(s => s.CreatedAtUtc, cancellationToken),
                LastAnalysisUtc = await _dbContext.AiAnalyses.MaxAsync(a => a.CreatedAtUtc, cancellationToken),
                LastPredictionUtc = await _dbContext.Predictions.MaxAsync(p => p.CreatedAtUtc, cancellationToken)
            };
            return summary;
        }

        public async Task<(IReadOnlyList<MarketAdminDto> Items, int Total)> GetMarketsAsync(int page, int pageSize, string? status, string? search, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.Markets.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.Equals("Active", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(m => m.Active);
                else if (status.Equals("Closed", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(m => m.Closed);
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(m => EF.Functions.ILike(m.Question, $"%{search}%"));
            }
            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderBy(m => m.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MarketAdminDto
                {
                    Id = m.Id,
                    Title = m.Question,
                    Category = m.Category.ToString(),
                    MarketProbability = m.Probability,
                    Status = m.Active ? "Active" : (m.Closed ? "Closed" : "Inactive"),
                    CreatedAtUtc = m.CreatedAtUtc,
                    CloseTimeUtc = m.Closed ? m.ResolvedAtUtc : (DateTimeOffset?)null
                })
                .ToListAsync(cancellationToken);
            return (items, total);
        }

        public async Task<(IReadOnlyList<PredictionAdminDto> Items, int Total)> GetPredictionsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.Predictions.AsNoTracking();
            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderBy(p => p.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PredictionAdminDto
                {
                    PredictionId = p.Id,
                    MarketTitle = p.Market.Question,
                    AiProbability = p.AiProbability,
                    Confidence = p.ConfidencePercentage,
                    CreatedAtUtc = p.CreatedAtUtc,
                    Resolved = p.EvaluatedAtUtc != null,
                    OutcomeCorrect = p.WasCorrect
                })
                .ToListAsync(cancellationToken);
            return (items, total);
        }

        public async Task<(IReadOnlyList<OpportunityAdminDto> Items, int Total)> GetOpportunitiesAsync(bool? hasEdge, decimal? minGap, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.PredictionOpportunities.AsNoTracking();
            if (hasEdge.HasValue)
                query = query.Where(o => o.HasEdge == hasEdge.Value);
            if (minGap.HasValue)
                query = query.Where(o => o.ProbabilityGap >= minGap.Value);
            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(o => o.DetectedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OpportunityAdminDto
                {
                    PredictionId = o.PredictionId,
                    MarketTitle = o.Prediction.Market.Question,
                    MarketProbability = o.Prediction.Market.Probability,
                    AiProbability = o.Prediction.AiProbability,
                    ProbabilityGap = o.ProbabilityGap,
                    GapDirection = o.AiProbability > o.MarketProbability ? "AIHigher" : "AILower",
                    HasEdge = o.HasEdge,
                    DetectedAtUtc = o.DetectedAtUtc
                })
                .ToListAsync(cancellationToken);
            return (items, total);
        }

        public async Task<QueueStatisticsDto> GetQueueStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var stats = new QueueStatisticsDto
            {
                PendingMarketAnalyses = await _dbContext.MarketAnalysisQueueItems.CountAsync(q => q.Status == Core.Enums.AnalysisQueueStatus.Pending, cancellationToken),
                CompletedMarketAnalyses = await _dbContext.MarketAnalysisQueueItems.CountAsync(q => q.Status == Core.Enums.AnalysisQueueStatus.Completed, cancellationToken),
                PendingPredictions = await _dbContext.Predictions.CountAsync(p => p.EvaluatedAtUtc == null, cancellationToken),
                CompletedPredictions = await _dbContext.Predictions.CountAsync(p => p.EvaluatedAtUtc != null, cancellationToken),
                FailedAnalyses = await _dbContext.MarketAnalysisQueueItems.CountAsync(q => q.Status == Core.Enums.AnalysisQueueStatus.Failed, cancellationToken),
                FailedPredictions = 0 // placeholder; could be derived from error logs if stored
            };
            return stats;
        }
    }
}
