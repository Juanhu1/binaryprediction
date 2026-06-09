using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Core.Repositories;
using BinaryPrediction.Core.Services;
using BinaryPrediction.Core.Common;
using BinaryPrediction.Infrastructure.External.Polymarket;
using BinaryPrediction.Infrastructure.Persistence;
using BinaryPrediction.Infrastructure.Repositories;
using BinaryPrediction.Infrastructure.Services;
using BinaryPrediction.Infrastructure.Services.Classification;
using BinaryPrediction.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BinaryPrediction.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BinaryPredictionDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(BinaryPredictionDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null);
                })
                .UseSnakeCaseNamingConvention();
        });

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        services.Configure<PolymarketSettings>(configuration.GetSection("Polymarket"));
        services.Configure<MarketFilteringSettings>(configuration.GetSection("MarketFilteringSettings"));
        services.Configure<QueueProcessingSettings>(configuration.GetSection("QueueProcessing"));
        services.Configure<WorkerSettings>(configuration.GetSection("Workers"));
        services.Configure<AnalysisRefreshSettings>(configuration.GetSection("AnalysisRefreshHours"));
        services.AddHttpClient<IPolymarketClient, PolymarketClient>((sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<PolymarketSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        });
        services.AddScoped<IMarketSelectionService, MarketSelectionService>();
        services.AddScoped<IEdgeCalculationService, EdgeCalculationService>();
        services.AddScoped<IMarketSynchronizationService, MarketSynchronizationService>();
        services.AddScoped<IMarketQueryService, MarketQueryService>();
        services.AddScoped<IMarketAnalysisQueueService, MarketAnalysisQueueService>();
        services.AddScoped<IAiAnalysisService, AiAnalysisService>();
        services.AddSingleton<IMarketQuestionNormalizer, MarketQuestionNormalizer>();
        services.AddSingleton<SportsClassifier>();
        services.AddSingleton<IMarketCategoryClassifier, MarketCategoryResolver>();
        services.AddSingleton<IMarketResolutionDateResolver, MarketResolutionDateResolver>();
        services.AddScoped<IMarketQualityScoringService, MarketQualityScoringService>();
        services.AddScoped<IMarketEligibilityService, MarketEligibilityService>();
        
        services.AddSingleton<IMockAnalysisGenerator, MockAnalysisGenerator>();
        services.AddSingleton<IMockPredictionGenerator, MockPredictionGenerator>();

        var openAiSection = configuration.GetSection("OpenAiSettings");
        var openAiSettings = openAiSection.Get<OpenAiSettings>();
        
        if (openAiSettings != null && !openAiSettings.UseMockAnalysis)
        {
            if (string.IsNullOrWhiteSpace(openAiSettings.ApiKey) || openAiSettings.ApiKey == "sk-mock-key-for-testing")
                throw new InvalidOperationException("OpenAI API Key is missing or invalid. System requires a valid API key when UseMockAnalysis is false.");
                
            if (string.IsNullOrWhiteSpace(openAiSettings.Model))
                throw new InvalidOperationException("OpenAI Model Name is missing.");
                
            if (openAiSettings.MaxAnalysesPerMinute <= 0 || openAiSettings.DailyAnalysisLimit <= 0)
                throw new InvalidOperationException("OpenAI Rate Limits must be greater than zero.");
        }

        services.Configure<OpenAiSettings>(openAiSection);
        services.AddTransient<IOpenAiRetryService, OpenAiRetryService>();
        services.AddTransient<IPromptService, PromptService>();
        services.AddTransient<IAiPerformanceService, AiPerformanceService>();
        services.AddHttpClient<IOpenAiAnalysisService, OpenAiAnalysisService>();
        
        services.AddScoped<IPredictionRepository, PredictionRepository>();
        services.AddScoped<IPredictionService, PredictionService>();
        services.AddScoped<IMarketResolutionService, MarketResolutionService>();
        services.AddScoped<IPredictionEvaluationService, PredictionEvaluationService>();
        services.AddScoped<IPredictionStatisticsService, PredictionStatisticsService>();
        services.AddScoped<IPredictionBenchmarkService, PredictionBenchmarkService>();
        services.AddScoped<IPredictionDashboardService, PredictionDashboardService>();
        services.AddScoped<IConfidenceBandService, ConfidenceBandService>();
        services.AddScoped<IMarketCategoryPerformanceService, MarketCategoryPerformanceService>();
        services.AddScoped<IPredictionQualityService, PredictionQualityService>();
        services.AddScoped<IPredictionsImprovementService, PredictionsImprovementService>();

        // Repositories
        services.AddScoped<IPredictionRepository, PredictionRepository>();
        services.AddScoped<IPredictionPerformanceRepository, PredictionPerformanceRepository>();

        return services;
    }
}
