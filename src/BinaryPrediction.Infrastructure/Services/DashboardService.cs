using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BinaryPrediction.Infrastructure.Persistence;
using BinaryPrediction.Core.Enums;

using BinaryPrediction.Core.DTOs.Dashboard;

namespace BinaryPrediction.Infrastructure.Services
{

public class DashboardService : IDashboardService
{
    private readonly BinaryPredictionDbContext _dbContext;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(BinaryPredictionDbContext dbContext, ILogger<DashboardService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<DashboardOverviewDto> GetOverviewAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var today = now.Date;

        var totalMarkets = await _dbContext.Markets.AsNoTracking().CountAsync(ct);
        var openMarkets = await _dbContext.Markets.AsNoTracking().CountAsync(m => !m.Closed && (m.EndDate == null || m.EndDate > now), ct);
        var resolvedMarkets = await _dbContext.Markets.AsNoTracking().CountAsync(m => m.Closed, ct);

        var totalPredictions = await _dbContext.Predictions.AsNoTracking().CountAsync(ct);
        var pendingPredictions = await _dbContext.Predictions.AsNoTracking().CountAsync(p => p.EvaluatedAtUtc == null, ct);
        var evaluatedPredictions = await _dbContext.Predictions.AsNoTracking().CountAsync(p => p.EvaluatedAtUtc != null, ct);

        var accuracy = await _dbContext.Predictions.AsNoTracking()
            .Where(p => p.EvaluatedAtUtc != null && p.WasCorrect != null)
            .AverageAsync(p => p.WasCorrect == true ? 1m : 0m, ct);
        var avgConfidence = await _dbContext.Predictions.AsNoTracking()
            .Where(p => p.EvaluatedAtUtc != null)
            .AverageAsync(p => p.ConfidencePercentage, ct);
        var avgError = await _dbContext.Predictions.AsNoTracking()
            .Where(p => p.EvaluatedAtUtc != null && p.PredictionError != null)
            .AverageAsync(p => p.PredictionError.Value, ct);

        var totalOpportunities = await _dbContext.PredictionOpportunities.AsNoTracking().CountAsync(ct);
        var activeOpportunities = await _dbContext.PredictionOpportunities.AsNoTracking()
            .CountAsync(o => o.Status == OpportunityStatus.Active, ct);
        var resolvedOpportunities = await _dbContext.PredictionOpportunities.AsNoTracking()
            .CountAsync(o => o.Status == OpportunityStatus.Resolved, ct);

        return new DashboardOverviewDto
        {
            TotalMarkets = totalMarkets,
            OpenMarkets = openMarkets,
            ResolvedMarkets = resolvedMarkets,
            TotalPredictions = totalPredictions,
            PendingPredictions = pendingPredictions,
            EvaluatedPredictions = evaluatedPredictions,
            AccuracyPercentage = Math.Round(accuracy * 100, 2),
            AverageConfidence = Math.Round(avgConfidence, 2),
            AveragePredictionError = Math.Round(avgError, 4),
            TotalOpportunities = totalOpportunities,
            ActiveOpportunities = activeOpportunities,
            ResolvedOpportunities = resolvedOpportunities
        };
    }

    public async Task<PaginatedResult<MarketDto>> GetMarketsAsync(DashboardMarketQuery query, CancellationToken ct = default)
    {
        var q = _dbContext.Markets.AsNoTracking().AsQueryable();
        // Filters
        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(m => EF.Functions.ILike(m.Question, $"%{query.Search}%"));
        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            if (Enum.TryParse<MarketCategory>(query.Category, true, out var cat))
                q = q.Where(m => m.Category == cat);
        }
        if (!string.IsNullOrWhiteSpace(query.Status))
            q = q.Where(m => (m.Closed ? "Resolved" : "Open") == query.Status);

        // Sorting
        q = (query.SortBy?.ToLower()) switch
        {
            "createddate" => query.SortDesc ? q.OrderByDescending(m => m.CreatedAtUtc) : q.OrderBy(m => m.CreatedAtUtc),
            "resolutiondate" => query.SortDesc ? q.OrderByDescending(m => m.ResolvedAtUtc) : q.OrderBy(m => m.ResolvedAtUtc),
            "enddate" => query.SortDesc ? q.OrderByDescending(m => m.EndDate) : q.OrderBy(m => m.EndDate),
            _ => q.OrderBy(m => m.Question)
        };

        var total = await q.CountAsync(ct);
        var items = await q.Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(m => new MarketDto
            {
                Id = m.Id,
                Question = m.Question,
                Category = m.Category.ToString(),
                Source = string.Empty,
                CreatedDate = m.CreatedAtUtc,
                ResolutionDate = m.ResolvedAtUtc,
                EndDate = m.EndDate,
                Status = m.Closed ? "Resolved" : "Open"
            })
            .ToListAsync(ct);

        return new PaginatedResult<MarketDto>
        {
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total,
            Items = items
        };
    }

    public async Task<PaginatedResult<PredictionDto>> GetPredictionsAsync(DashboardPredictionQuery query, CancellationToken ct = default)
    {
        var q = _dbContext.Predictions.AsNoTracking().Include(p => p.Market).AsQueryable();

        // Search
        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(p => EF.Functions.ILike(p.Market.Question, $"%{query.Search}%"));

        // Filters
        if (query.PendingOnly)
            q = q.Where(p => p.EvaluatedAtUtc == null);
        if (query.EvaluatedOnly)
            q = q.Where(p => p.EvaluatedAtUtc != null);
        if (!string.IsNullOrWhiteSpace(query.Category))
            q = q.Where(p => p.Market != null && p.Market.Category != null && p.Market.Category.ToString() == query.Category);
        if (query.ConfidenceMin.HasValue)
            q = q.Where(p => p.ConfidencePercentage >= query.ConfidenceMin.Value);
        if (query.ConfidenceMax.HasValue)
            q = q.Where(p => p.ConfidencePercentage <= query.ConfidenceMax.Value);

        // Sorting
        q = (query.SortBy?.ToLower()) switch
        {
            "confidence" => query.SortDesc ? q.OrderByDescending(p => p.ConfidencePercentage) : q.OrderBy(p => p.ConfidencePercentage),
            "createddate" => query.SortDesc ? q.OrderByDescending(p => p.CreatedAtUtc) : q.OrderBy(p => p.CreatedAtUtc),
            "evaluateddate" => query.SortDesc ? q.OrderByDescending(p => p.EvaluatedAtUtc) : q.OrderBy(p => p.EvaluatedAtUtc),
            "predictionerror" => query.SortDesc ? q.OrderByDescending(p => p.PredictionError) : q.OrderBy(p => p.PredictionError),
            _ => q.OrderBy(p => p.Id)
        };

        var total = await q.CountAsync(ct);
        var items = await q.Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new PredictionDto
            {
                Id = p.Id,
                MarketId = p.MarketId,
                Question = p.Market != null ? p.Market.Question : string.Empty,
                Category = p.Market.Category.ToString(),
                ConfidencePercentage = p.ConfidencePercentage,
                CreatedDate = p.CreatedAtUtc,
                EvaluatedDate = p.EvaluatedAtUtc,
                PredictedOutcome = p.PredictedOutcome,
                PredictionError = p.PredictionError
            })
            .ToListAsync(ct);

        return new PaginatedResult<PredictionDto>
        {
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total,
            Items = items
        };
    }

    public async Task<PaginatedResult<OpportunityDto>> GetOpportunitiesAsync(DashboardOpportunityQuery query, CancellationToken ct = default)
    {
        var q = _dbContext.PredictionOpportunities.AsNoTracking().Include(o => o.Market).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (Enum.TryParse<OpportunityStatus>(query.Status, true, out var statusEnum))
                q = q.Where(o => o.Status == statusEnum);
        }
        if (query.MinGap.HasValue)
            q = q.Where(o => o.ProbabilityGap >= query.MinGap.Value);
        if (query.MaxGap.HasValue)
            q = q.Where(o => o.ProbabilityGap <= query.MaxGap.Value);

        // Sorting
        q = (query.SortBy?.ToLower()) switch
        {
            "gappercentage" => query.SortDesc ? q.OrderByDescending(o => o.ProbabilityGap) : q.OrderBy(o => o.ProbabilityGap),
            "detecteddt" => query.SortDesc ? q.OrderByDescending(o => o.DetectedAtUtc) : q.OrderBy(o => o.DetectedAtUtc),
            _ => q.OrderBy(o => o.Id)
        };

        var total = await q.CountAsync(ct);
        var items = await q.Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(o => new OpportunityDto
            {
                Id = o.Id,
                PredictionId = o.PredictionId,
                MarketId = o.MarketId,
                Question = o.Market != null ? o.Market.Question : string.Empty,
                Category = o.Market != null ? o.Market.Category.ToString() : string.Empty,
                MarketSlug = o.Market != null ? o.Market.Slug : string.Empty,
                MarketProbability = o.Market != null ? o.Market.Probability : 0m,
                AiProbability = o.AiProbability,
                ProbabilityGap = o.ProbabilityGap,
                Direction = o.GapDirection.ToString(),
                HasEdge = o.HasEdge,
                DetectedAtUtc = o.DetectedAtUtc,
                PolymarketUrl = o.Market != null ? $"https://polymarket.com/market/{o.Market.Slug}" : string.Empty,
                EndDate = o.Market != null ? o.Market.EndDate : null
            })
            .ToListAsync(ct);

        return new PaginatedResult<OpportunityDto>
        {
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total,
            Items = items
        };
    }

    public async Task<AnalyticsDto> GetAnalyticsAsync(CancellationToken ct = default)
    {
        // Confidence distribution buckets
        var buckets = await _dbContext.Predictions.AsNoTracking()
            .Where(p => p.EvaluatedAtUtc != null)
            .GroupBy(p => (int)(p.ConfidencePercentage / 10))
            .Select(g => new ConfidenceBucketDto
            {
                RangeStart = g.Key * 10,
                RangeEnd = g.Key * 10 + 9,
                Count = g.Count()
            })
            .OrderBy(b => b.RangeStart)
            .ToListAsync(ct);

        var accuracy = await _dbContext.Predictions.AsNoTracking()
            .Where(p => p.EvaluatedAtUtc != null && p.WasCorrect != null)
            .AverageAsync(p => p.WasCorrect == true ? 1m : 0m, ct);
        var avgConfidence = await _dbContext.Predictions.AsNoTracking()
            .Where(p => p.EvaluatedAtUtc != null)
            .AverageAsync(p => p.ConfidencePercentage, ct);
        var avgError = await _dbContext.Predictions.AsNoTracking()
            .Where(p => p.EvaluatedAtUtc != null && p.PredictionError != null)
            .AverageAsync(p => p.PredictionError.Value, ct);

        var totalEvaluated = await _dbContext.Predictions.AsNoTracking()
            .Where(p => p.EvaluatedAtUtc != null)
            .CountAsync(ct);
        var correctCount = await _dbContext.Predictions.AsNoTracking()
            .Where(p => p.EvaluatedAtUtc != null && p.WasCorrect == true)
            .CountAsync(ct);
        var incorrectCount = await _dbContext.Predictions.AsNoTracking()
            .Where(p => p.EvaluatedAtUtc != null && p.WasCorrect == false)
            .CountAsync(ct);

        var calibration = await _dbContext.PredictionCalibrationSnapshots.AsNoTracking()
            .Select(s => new CalibrationSnapshotDto
            {
                ConfidenceRange = s.ConfidenceRange,
                PredictionCount = s.PredictionCount,
                ExpectedAccuracy = s.ExpectedAccuracyPercentage,
                ActualAccuracy = s.ActualAccuracyPercentage,
                CalibrationError = s.CalibrationError
            })
            .ToListAsync(ct);

        return new AnalyticsDto
        {
            AccuracyPercentage = Math.Round(accuracy * 100, 2),
            AverageConfidence = Math.Round(avgConfidence, 2),
            AveragePredictionError = Math.Round(avgError, 4),
            ConfidenceBuckets = buckets,
            CalibrationSnapshots = calibration,
            TotalEvaluated = totalEvaluated,
            CorrectCount = correctCount,
            IncorrectCount = incorrectCount
        };
    }

    public async Task<SystemDto> GetSystemAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var today = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);

        var totalMarkets = await _dbContext.Markets.AsNoTracking().CountAsync(ct);
        var marketsAddedToday = await _dbContext.Markets.AsNoTracking()
            .CountAsync(m => m.CreatedAtUtc >= today, ct);

        var totalPredictions = await _dbContext.Predictions.AsNoTracking().CountAsync(ct);
        var predictionsGeneratedToday = await _dbContext.Predictions.AsNoTracking()
            .CountAsync(p => p.CreatedAtUtc >= today, ct);
        var predictionsEvaluatedToday = await _dbContext.Predictions.AsNoTracking()
            .CountAsync(p => p.EvaluatedAtUtc >= today, ct);

        var totalOpportunities = await _dbContext.PredictionOpportunities.AsNoTracking().CountAsync(ct);
        var opportunitiesDetectedToday = await _dbContext.PredictionOpportunities.AsNoTracking()
            .CountAsync(o => o.DetectedAtUtc >= today, ct);

        var latestAnalyticsSnapshot = await _dbContext.OpportunityAnalyticsSnapshots.AsNoTracking()
            .OrderByDescending(s => s.SnapshotDateUtc)
            .Select(s => (DateTimeOffset?)s.SnapshotDateUtc.ToDateTime(TimeOnly.MinValue))
            .FirstOrDefaultAsync(ct);

        return new SystemDto
        {
            TotalMarkets = totalMarkets,
            MarketsAddedToday = marketsAddedToday,
            TotalPredictions = totalPredictions,
            PredictionsGeneratedToday = predictionsGeneratedToday,
            PredictionsEvaluatedToday = predictionsEvaluatedToday,
            TotalOpportunities = totalOpportunities,
            OpportunitiesDetectedToday = opportunitiesDetectedToday,
            LatestAnalyticsSnapshotDate = latestAnalyticsSnapshot
        };
    }

        // Retrieves detailed prediction information including analysis sections
        public async Task<PredictionDetailsDto?> GetPredictionDetailsAsync(Guid predictionId, CancellationToken ct = default)
        {
            var prediction = await _dbContext.Predictions
                .AsNoTracking()
                .Include(p => p.Market)
                .FirstOrDefaultAsync(p => p.Id == predictionId, ct);
            if (prediction == null) return null; // No prediction found

            var analysis = await _dbContext.AiAnalyses
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.MarketId == prediction.MarketId, ct);

            var details = new PredictionDetailsDto
            {
                PredictionId = prediction.Id,
                MarketId = prediction.MarketId,
                Question = prediction.Market?.Question ?? string.Empty,
                Category = prediction.Market?.Category.ToString() ?? string.Empty,
                PredictedOutcome = prediction.PredictedOutcome,
                ConfidencePercentage = prediction.ConfidencePercentage,
                CreatedDate = prediction.CreatedAtUtc,
                EvaluatedDate = prediction.EvaluatedAtUtc,
                ActualOutcome = prediction.ActualOutcome,
                PredictionError = prediction.PredictionError,
                WasCorrect = prediction.WasCorrect,
                MarketSummary = analysis?.Summary ?? string.Empty,
                SupportingEvidence = analysis != null ? JsonSerializer.Deserialize<List<string>>(analysis.KeyReasonsJson) ?? new() : new(),
                ContradictingEvidence = analysis != null ? JsonSerializer.Deserialize<List<string>>(analysis.RiskFactorsJson) ?? new() : new(),
                KeyRisks = new List<string>(),
                ConfidenceExplanation = analysis?.Summary ?? string.Empty,
                FinalProbability = analysis?.EstimatedProbability ?? 0m
            };
            return details;
        }
    }
}
