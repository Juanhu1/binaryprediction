using BinaryPrediction.Infrastructure.Extensions;
using BinaryPrediction.Worker.Extensions;
using BinaryPrediction.Worker.Jobs;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog
builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/binaryprediction-worker-.txt", rollingInterval: RollingInterval.Day));

builder.Services.AddWorkerServices(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

// Background Workers
builder.Services.AddHostedService<MarketCollectorWorker>();
// We keep AiAnalysisWorker disabled or intact per instructions to not rewrite existing, 
// but we will register our new queue workers:
builder.Services.AddHostedService<MarketAnalysisQueueWorker>();
builder.Services.AddHostedService<MarketAnalysisProcessorWorker>();
builder.Services.AddHostedService<MarketMaintenanceWorker>();
builder.Services.AddHostedService<PredictionWorker>();
builder.Services.AddHostedService<PredictionResolutionWorker>();
builder.Services.AddHostedService<PredictionEvaluationWorker>();
builder.Services.AddHostedService<PredictionQualityWorker>();
builder.Services.AddHostedService<SystemHealthWorker>();

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Queue processor enabled");
logger.LogInformation("Queue creator enabled");
logger.LogInformation("Legacy analysis workers disabled");

var hostedServices = host.Services.GetServices<IHostedService>();
logger.LogInformation("Registered Hosted Services:");
foreach (var s in hostedServices)
{
    logger.LogInformation("- {ServiceName}", s.GetType().Name);
}

host.Run();
