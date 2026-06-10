using BinaryPrediction.Core.Common;
using BinaryPrediction.Worker.Jobs;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BinaryPrediction.Worker.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddWorkerServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenAiSettings>(configuration.GetSection("OpenAiSettings"));
        services.Configure<AnalysisSettings>(configuration.GetSection("AnalysisSettings"));
        services.AddScoped<IPerformanceSnapshotService, PerformanceSnapshotService>();
        services.AddHostedService<DailyAnalyticsWorker>();
        return services;
    }
}
