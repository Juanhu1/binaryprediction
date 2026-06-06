using BinaryPrediction.Core.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BinaryPrediction.Worker.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddWorkerServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenAiSettings>(configuration.GetSection("OpenAiSettings"));
        services.Configure<AnalysisSettings>(configuration.GetSection("AnalysisSettings"));
        return services;
    }
}
