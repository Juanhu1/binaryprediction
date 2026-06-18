using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Enums;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Core.Options;
using BinaryPrediction.Core.Common;
using BinaryPrediction.Core.DTOs.Dashboard;
using BinaryPrediction.Infrastructure.Persistence;
using BinaryPrediction.Infrastructure.Persistence.Repositories;
using BinaryPrediction.Infrastructure.Repositories;
using BinaryPrediction.Infrastructure.Services;
using BinaryPrediction.Infrastructure.Services.Classification;
using BinaryPrediction.Infrastructure.External.Kalshi;
using BinaryPrediction.Infrastructure.External.Polymarket;
using BinaryPrediction.Infrastructure.External.Polymarket.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BinaryPrediction.Infrastructure.Tests;

public class KalshiIntegrationTests : IDisposable
{
    private readonly BinaryPredictionDbContext _dbContext;
    private readonly TestKalshiClient _kalshiClient;
    private readonly TestPolymarketClient _polymarketClient;
    private readonly MarketSynchronizationService _syncService;
    private readonly DashboardService _dashboardService;
    private readonly EdgeDetectionService _edgeService;

    public KalshiIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<BinaryPredictionDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new TestBinaryPredictionDbContext(options);
        _dbContext.Database.EnsureCreated();

        _kalshiClient = new TestKalshiClient();
        _polymarketClient = new TestPolymarketClient();

        var marketRepo = new Repository<Market>(_dbContext);
        var snapshotRepo = new Repository<MarketSnapshot>(_dbContext);
        var normalizer = new MarketQuestionNormalizer();
        var sportsClassifier = new SportsClassifier(NullLogger<SportsClassifier>.Instance);
        var categoryResolver = new MarketCategoryResolver(sportsClassifier);
        var scoringService = new MarketQualityScoringService(categoryResolver);
        
        var filteringSettings = Microsoft.Extensions.Options.Options.Create(new MarketFilteringSettings
        {
            MinimumLiquidity = 0m,
            MinimumVolume = 0m,
            MinimumQualityScore = 0m,
            EligibleCategories = Enum.GetValues<MarketCategory>().ToList(),
            MaximumMarketDurationDays = 1000
        });
        var eligibilityService = new MarketEligibilityService(filteringSettings, NullLogger<MarketEligibilityService>.Instance);
        
        var dateResolver = new MarketResolutionDateResolver();
        var edgeOptions = Microsoft.Extensions.Options.Options.Create(new EdgeDetectionOptions { GapThresholdPercentage = 5 });

        _syncService = new MarketSynchronizationService(
            _polymarketClient,
            _kalshiClient,
            marketRepo,
            snapshotRepo,
            normalizer,
            scoringService,
            eligibilityService,
            dateResolver,
            NullLogger<MarketSynchronizationService>.Instance,
            _dbContext,
            edgeOptions);

        _dashboardService = new DashboardService(_dbContext, NullLogger<DashboardService>.Instance);

        var predictionRepo = new PredictionRepository(_dbContext);
        var opportunityRepo = new PredictionOpportunityRepository(_dbContext);
        var historyRepo = new OpportunityStatusHistoryRepository(_dbContext);
        var lifecycleService = new OpportunityLifecycleService(opportunityRepo, historyRepo, NullLogger<OpportunityLifecycleService>.Instance);
        _edgeService = new EdgeDetectionService(predictionRepo, opportunityRepo, lifecycleService, edgeOptions, NullLogger<EdgeDetectionService>.Instance);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task KalshiMarketImport_StoresProbabilityAsZeroToOne()
    {
        // Arrange
        var kalshiMarket = new KalshiMarketDto
        {
            Ticker = "TEST-TICKER",
            EventTicker = "TEST-EVENT",
            Title = "Will the Fed cut rates in July?",
            Status = "open",
            YesBidDollars = 0.65m,
            YesAskDollars = 0.75m,
            VolumeFp = 1000,
            LiquidityDollars = 500,
            CloseTime = DateTimeOffset.UtcNow.AddDays(10)
        };
        _kalshiClient.Markets.Add(kalshiMarket);

        // Act
        await _syncService.SynchronizeKalshiMarketsAsync();

        // Assert
        var market = await _dbContext.Markets.FirstOrDefaultAsync(m => m.MarketSource == MarketSource.Kalshi && m.ExternalMarketId == "TEST-TICKER");
        Assert.NotNull(market);
        Assert.Equal(0.70m, market.Probability); // Midpoint of 0.65 and 0.75 is 0.70
        Assert.Equal("kalshi-test-ticker", market.Slug);
        Assert.Equal("https://kalshi.com/markets/TEST-TICKER", market.SourceUrl);
        Assert.True(market.EligibleForAnalysis);
    }

    [Fact]
    public async Task PolymarketAndKalshiMarkets_WithSimilarQuestion_DoNotOverwriteEachOther()
    {
        // Arrange
        // Seed Polymarket market
        var polyMarket = new PolymarketMarketDto
        {
            Slug = "cavaliers-win-finals",
            Question = "Will the Cavaliers win the NBA Finals?",
            Active = true,
            Closed = false,
            OutcomePrices = new List<string> { "0.55", "0.45" },
            Outcomes = new List<string> { "Yes", "No" },
            Liquidity = 1000m,
            Volume = 5000m,
            EndDate = DateTimeOffset.UtcNow.AddDays(15)
        };
        _polymarketClient.Markets.Add(polyMarket);

        // Seed Kalshi market with similar normalized question
        var kalshiMarket = new KalshiMarketDto
        {
            Ticker = "CAVS-FINALS",
            EventTicker = "NBA-2026",
            Title = "Will Cleveland Cavaliers win the NBA Finals?",
            Status = "open",
            LastPriceDollars = 0.60m,
            VolumeFp = 2000,
            LiquidityDollars = 1500,
            CloseTime = DateTimeOffset.UtcNow.AddDays(15)
        };
        _kalshiClient.Markets.Add(kalshiMarket);

        // Act - Synchronize both sources
        await _syncService.SynchronizeActiveMarketsAsync();
        await _syncService.SynchronizeKalshiMarketsAsync();

        // Assert
        var allMarkets = await _dbContext.Markets.ToListAsync();
        Assert.Equal(2, allMarkets.Count);

        var pm = allMarkets.FirstOrDefault(m => m.MarketSource == MarketSource.Polymarket);
        var km = allMarkets.FirstOrDefault(m => m.MarketSource == MarketSource.Kalshi);

        Assert.NotNull(pm);
        Assert.NotNull(km);
        Assert.Equal("cavaliers-win-finals", pm.Slug);
        Assert.Equal("kalshi-cavs-finals", km.Slug);
        Assert.Equal(0.55m, pm.Probability);
        Assert.Equal(0.60m, km.Probability);
    }

    [Fact]
    public async Task GetOpportunitiesAsync_ReturnsBothSources_FiltersBySource_AndDeduplicates()
    {
        // Arrange
        var pmMarket = new Market
        {
            Id = Guid.NewGuid(),
            Question = "Polymarket Question",
            MarketSource = MarketSource.Polymarket,
            Slug = "pm-question",
            Probability = 0.50m,
            Active = true
        };
        var kmMarket = new Market
        {
            Id = Guid.NewGuid(),
            Question = "Kalshi Question",
            MarketSource = MarketSource.Kalshi,
            Slug = "kalshi-question",
            Probability = 0.60m,
            Active = true
        };
        _dbContext.Markets.AddRange(pmMarket, kmMarket);

        var pmPred = new Prediction { Id = Guid.NewGuid(), MarketId = pmMarket.Id, ConfidencePercentage = 80m, AiProbability = 80m, PromptVersionUsed = "v2" };
        var kmPredOlder = new Prediction { Id = Guid.NewGuid(), MarketId = kmMarket.Id, ConfidencePercentage = 40m, AiProbability = 40m, PromptVersionUsed = "v2" };
        var kmPred = new Prediction { Id = Guid.NewGuid(), MarketId = kmMarket.Id, ConfidencePercentage = 90m, AiProbability = 90m, PromptVersionUsed = "v2" };
        
        _dbContext.Predictions.AddRange(pmPred, kmPredOlder, kmPred);
        await _dbContext.SaveChangesAsync();

        // Detect opportunities in chronological order: older first, then newer
        await _edgeService.DetectOpportunityAsync(pmPred.Id);
        await _edgeService.DetectOpportunityAsync(kmPredOlder.Id);
        await _edgeService.DetectOpportunityAsync(kmPred.Id); // This should auto-expire the kmPredOlder opportunity

        // Manually adjust the DetectedAtUtc timestamps to guarantee correct ordering for dashboard queries
        var opps = await _dbContext.PredictionOpportunities.ToListAsync();
        var latestOpp = opps.First(o => o.PredictionId == kmPred.Id);
        var olderOpp = opps.First(o => o.PredictionId == kmPredOlder.Id);

        latestOpp.DetectedAtUtc = DateTimeOffset.UtcNow.AddMinutes(5);
        olderOpp.DetectedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5);
        await _dbContext.SaveChangesAsync();

        // Act 1: Get all active opportunities (default is Open or Active)
        var queryAll = new DashboardOpportunityQuery { Page = 1, PageSize = 10, HideZeroLiquidity = false, HideZeroVolume = false, HideZeroProbability = false };
        var resultAll = await _dashboardService.GetOpportunitiesAsync(queryAll);

        // Assert 1: Deduplication should return exactly 2 items (1 latest for Polymarket, 1 latest for Kalshi)
        Assert.Equal(2, resultAll.Items.Count);
        var pmResult = resultAll.Items.FirstOrDefault(i => i.MarketSource == MarketSource.Polymarket);
        var kmResult = resultAll.Items.FirstOrDefault(i => i.MarketSource == MarketSource.Kalshi);

        Assert.NotNull(pmResult);
        Assert.NotNull(kmResult);
        Assert.Equal(pmPred.Id, pmResult.PredictionId);
        Assert.Equal(kmPred.Id, kmResult.PredictionId); // should be the latest prediction ID for Kalshi

        // Act 2: Filter by Polymarket
        var queryPM = new DashboardOpportunityQuery { Page = 1, PageSize = 10, Source = "Polymarket", HideZeroLiquidity = false, HideZeroVolume = false, HideZeroProbability = false };
        var resultPM = await _dashboardService.GetOpportunitiesAsync(queryPM);

        // Assert 2: Only Polymarket
        Assert.Single(resultPM.Items);
        Assert.Equal(MarketSource.Polymarket, resultPM.Items[0].MarketSource);

        // Act 3: Filter by Kalshi
        var queryKM = new DashboardOpportunityQuery { Page = 1, PageSize = 10, Source = "Kalshi", HideZeroLiquidity = false, HideZeroVolume = false, HideZeroProbability = false };
        var resultKM = await _dashboardService.GetOpportunitiesAsync(queryKM);

        // Assert 3: Only Kalshi
        Assert.Single(resultKM.Items);
        Assert.Equal(MarketSource.Kalshi, resultKM.Items[0].MarketSource);
    }

    [Fact]
    public async Task EnqueueEligibleMarkets_WithCapAndStableOrder_EnqueuesAllEventually()
    {
        // Arrange
        var baseTime = DateTimeOffset.UtcNow.AddDays(-10);
        
        // Seed 5 eligible markets with different CreatedAtUtc times
        for (int i = 0; i < 5; i++)
        {
            var marketId = Guid.NewGuid();
            var m = new Market
            {
                Id = marketId,
                Question = $"Test Question {i}",
                Slug = $"test-question-{i}",
                MarketSource = MarketSource.Kalshi,
                Active = true,
                EligibleForAnalysis = true,
                CreatedAtUtc = baseTime.AddHours(i)
            };
            _dbContext.Markets.Add(m);

            var ev = new EligibleMarketView
            {
                Id = marketId,
                Question = $"Test Question {i}",
                Category = MarketCategory.Politics,
                CreatedAtUtc = baseTime.AddHours(i),
                UpdatedAtUtc = baseTime.AddHours(i)
            };
            _dbContext.EligibleMarketsView.Add(ev);
        }
        await _dbContext.SaveChangesAsync();

        var queueSettings = Microsoft.Extensions.Options.Options.Create(new AnalysisQueueSettings
        {
            MaxMarketsToQueuePerRun = 2
        });
        
        var queueProcessingSettings = Microsoft.Extensions.Options.Options.Create(new QueueProcessingSettings
        {
            MaxRetries = 3
        });

        var refreshSettings = Microsoft.Extensions.Options.Options.Create(new AnalysisRefreshSettings
        {
            Politics = 24,
            Other = 24
        });

        var queueService = new MarketAnalysisQueueService(
            _dbContext,
            NullLogger<MarketAnalysisQueueService>.Instance,
            queueProcessingSettings,
            refreshSettings,
            queueSettings);

        // Act 1: First run - should enqueue the oldest 2 (Test Question 0 and Test Question 1)
        await queueService.EnqueueEligibleMarketsAsync();

        // Assert 1
        var queuedItems = await _dbContext.MarketAnalysisQueueItems
            .OrderBy(q => q.CreatedAtUtc)
            .ToListAsync();
            
        Assert.Equal(2, queuedItems.Count);
        
        // Verify that the enqueued markets are indeed the oldest 2
        var oldestMarketIds = await _dbContext.Markets
            .OrderBy(m => m.CreatedAtUtc)
            .Take(2)
            .Select(m => m.Id)
            .ToListAsync();
            
        Assert.Contains(queuedItems[0].MarketId, oldestMarketIds);
        Assert.Contains(queuedItems[1].MarketId, oldestMarketIds);

        // Act 2: Second run - should enqueue the next 2 (Test Question 2 and Test Question 3)
        await queueService.EnqueueEligibleMarketsAsync();

        // Assert 2
        queuedItems = await _dbContext.MarketAnalysisQueueItems
            .OrderBy(q => q.CreatedAtUtc)
            .ToListAsync();
            
        Assert.Equal(4, queuedItems.Count);

        // Act 3: Third run - should enqueue the last 1 (Test Question 4)
        await queueService.EnqueueEligibleMarketsAsync();

        // Assert 3
        queuedItems = await _dbContext.MarketAnalysisQueueItems
            .OrderBy(q => q.CreatedAtUtc)
            .ToListAsync();
            
        Assert.Equal(5, queuedItems.Count);
    }

    [Fact]
    public async Task KalshiEligibilityAndZeroValueFilters_WorkCorrectly()
    {
        // 1. Test Eligibility Logic
        var filteringSettings = new MarketFilteringSettings
        {
            MinimumLiquidity = 1000m,
            MinimumVolume = 1000m,
            KalshiMinimumVolume = 100m,
            MinimumQualityScore = 50m,
            EligibleCategories = new() { MarketCategory.Sports },
            MaximumMarketDurationDays = 365
        };
        var eligibilityService = new MarketEligibilityService(
            Microsoft.Extensions.Options.Options.Create(filteringSettings),
            NullLogger<MarketEligibilityService>.Instance);

        // Good Kalshi market
        var goodKalshi = new Market
        {
            MarketSource = MarketSource.Kalshi,
            ExternalMarketId = "NORMAL_TICKER",
            Question = "Will England win?",
            Category = MarketCategory.Sports,
            Active = true,
            Closed = false,
            Probability = 0.5m,
            Volume = 150m,
            Liquidity = 0m,
            QualityScore = 60,
            EndDate = DateTimeOffset.UtcNow.AddDays(10)
        };
        Assert.True(eligibilityService.EvaluateEligibility(goodKalshi, out _));

        // Parlay/combo family exclusion
        var parlayKalshi = new Market
        {
            MarketSource = MarketSource.Kalshi,
            ExternalMarketId = "KXMVESPORTSMULTIGAMEEXTENDED-S123",
            Question = "yes Miguel Vargas: 1+, yes Cody Bellinger: 1+",
            Category = MarketCategory.Sports,
            Active = true,
            Closed = false,
            Probability = 0.5m,
            Volume = 150m,
            Liquidity = 0m,
            QualityScore = 60,
            EndDate = DateTimeOffset.UtcNow.AddDays(10)
        };
        Assert.False(eligibilityService.EvaluateEligibility(parlayKalshi, out var parlayReason));
        Assert.Equal("Exotic parlay/combo market families are excluded.", parlayReason);

        // Low volume exclusion
        var lowVolumeKalshi = new Market
        {
            MarketSource = MarketSource.Kalshi,
            ExternalMarketId = "NORMAL_TICKER",
            Question = "Will England win?",
            Category = MarketCategory.Sports,
            Active = true,
            Closed = false,
            Probability = 0.5m,
            Volume = 50m,
            Liquidity = 0m,
            QualityScore = 60,
            EndDate = DateTimeOffset.UtcNow.AddDays(10)
        };
        Assert.False(eligibilityService.EvaluateEligibility(lowVolumeKalshi, out var volReason));
        Assert.Equal("Volume below minimum threshold.", volReason);

        // Zero probability exclusion
        var zeroProbKalshi = new Market
        {
            MarketSource = MarketSource.Kalshi,
            ExternalMarketId = "NORMAL_TICKER",
            Question = "Will England win?",
            Category = MarketCategory.Sports,
            Active = true,
            Closed = false,
            Probability = 0.0m,
            Volume = 150m,
            Liquidity = 0m,
            QualityScore = 60,
            EndDate = DateTimeOffset.UtcNow.AddDays(10)
        };
        Assert.False(eligibilityService.EvaluateEligibility(zeroProbKalshi, out var probReason));
        Assert.Equal("Market probability must be greater than 0 and less than 1.", probReason);

        // 2. Test Edge Detection Pricing validation
        // Create prediction and detect opportunity for zero-probability market
        var zeroMarket = new Market
        {
            Id = Guid.NewGuid(),
            MarketSource = MarketSource.Kalshi,
            ExternalMarketId = "NORMAL_TICKER",
            Question = "Will England win?",
            Category = MarketCategory.Sports,
            Active = true,
            Closed = false,
            Probability = 0.0m,
            Volume = 150m,
            Liquidity = 0m,
            QualityScore = 60,
            EndDate = DateTimeOffset.UtcNow.AddDays(10)
        };
        _dbContext.Markets.Add(zeroMarket);
        var pred = new Prediction
        {
            Id = Guid.NewGuid(),
            MarketId = zeroMarket.Id,
            ConfidencePercentage = 90m,
            AiProbability = 90m,
            PromptVersionUsed = "v2"
        };
        _dbContext.Predictions.Add(pred);
        await _dbContext.SaveChangesAsync();

        await _edgeService.DetectOpportunityAsync(pred.Id);

        // Opportunity should exist but HasEdge must be false due to invalid pricing
        var opp = await _dbContext.PredictionOpportunities.FirstOrDefaultAsync(o => o.PredictionId == pred.Id);
        Assert.NotNull(opp);
        Assert.False(opp.HasEdge);

        // 3. Test Dashboard Zero-Value Filters
        var normalMarket = new Market
        {
            Id = Guid.NewGuid(),
            MarketSource = MarketSource.Kalshi,
            ExternalMarketId = "NORMAL_TICKER_2",
            Question = "Normal Market?",
            Category = MarketCategory.Sports,
            Active = true,
            Closed = false,
            Probability = 0.5m,
            Volume = 200m,
            Liquidity = 0m,
            QualityScore = 60,
            EndDate = DateTimeOffset.UtcNow.AddDays(10)
        };
        _dbContext.Markets.Add(normalMarket);
        var normalPred = new Prediction
        {
            Id = Guid.NewGuid(),
            MarketId = normalMarket.Id,
            ConfidencePercentage = 90m,
            AiProbability = 90m,
            PromptVersionUsed = "v2"
        };
        _dbContext.Predictions.Add(normalPred);
        await _dbContext.SaveChangesAsync();

        // Detect opportunity for normal market
        await _edgeService.DetectOpportunityAsync(normalPred.Id);

        // Update the opportunity to status Open so it is picked up by default dashboard query
        var normalOpp = await _dbContext.PredictionOpportunities.FirstOrDefaultAsync(o => o.PredictionId == normalPred.Id);
        Assert.NotNull(normalOpp);
        normalOpp.Status = OpportunityStatus.Open;
        normalOpp.HasEdge = true; // explicitly force edge so dashboard picks it up
        await _dbContext.SaveChangesAsync();

        // Query with default filters (hide zero liquidity = true, which shouldn't hide Kalshi since Kalshi is allowed, but we hide zero volume/probability)
        var query = new DashboardOpportunityQuery
        {
            Page = 1,
            PageSize = 10,
            HideZeroLiquidity = true,
            HideZeroVolume = true,
            HideZeroProbability = true
        };
        var dashboardRes = await _dashboardService.GetOpportunitiesAsync(query);
        
        // Should return only the normal market opportunity, not the zero-probability one
        Assert.Contains(dashboardRes.Items, i => i.PredictionId == normalPred.Id);
        Assert.DoesNotContain(dashboardRes.Items, i => i.PredictionId == pred.Id);
    }
}

public class TestKalshiClient : IKalshiClient
{
    public List<KalshiMarketDto> Markets { get; set; } = new();
    
    public Task<IReadOnlyList<KalshiMarketDto>> GetActiveMarketsAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<KalshiMarketDto>>(Markets);
    }
}

public class TestPolymarketClient : IPolymarketClient
{
    public List<PolymarketMarketDto> Markets { get; set; } = new();

    public Task<IReadOnlyList<PolymarketMarketDto>> GetActiveMarketsAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PolymarketMarketDto>>(Markets);
    }

    public Task<PolymarketMarketDto?> GetMarketAsync(string slug, CancellationToken cancellationToken)
    {
        var market = Markets.FirstOrDefault(m => m.Slug == slug);
        return Task.FromResult(market);
    }
}

public class TestBinaryPredictionDbContext : BinaryPredictionDbContext
{
    public TestBinaryPredictionDbContext(DbContextOptions<BinaryPredictionDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<EligibleMarketView>().HasKey(e => e.Id);
    }
}
