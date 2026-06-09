using BinaryPrediction.Core.Common;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BinaryPrediction.Infrastructure.Services;

public class AiAnalysisService : IAiAnalysisService
{
    private readonly IOpenAiAnalysisService _openAiService;
    private readonly IEdgeCalculationService _edgeCalculationService;
    private readonly BinaryPredictionDbContext _dbContext;
    private readonly ILogger<AiAnalysisService> _logger;
    private readonly OpenAiSettings _openAiSettings;

    public AiAnalysisService(
        IOpenAiAnalysisService openAiService,
        IEdgeCalculationService edgeCalculationService,
        BinaryPredictionDbContext dbContext,
        ILogger<AiAnalysisService> logger,
        IOptions<OpenAiSettings> openAiOptions)
    {
        _openAiService = openAiService;
        _edgeCalculationService = edgeCalculationService;
        _dbContext = dbContext;
        _logger = logger;
        _openAiSettings = openAiOptions.Value;
    }

    public async Task ProcessMarketAsync(Market market, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting AI analysis for market {MarketId}.", market.Id);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var analysisResult = await _openAiService.AnalyzeMarketAsync(market, cancellationToken);
            stopwatch.Stop();

            if (analysisResult != null)
            {
                var analysis = _edgeCalculationService.CalculateEdge(
                    market, 
                    analysisResult.EstimatedProbability, 
                    analysisResult.Confidence);

                analysis.Summary = analysisResult.Summary;
                analysis.KeyReasonsJson = System.Text.Json.JsonSerializer.Serialize(analysisResult.KeyReasons);
                analysis.RiskFactorsJson = System.Text.Json.JsonSerializer.Serialize(analysisResult.RiskFactors);
                
                analysis.ModelName = _openAiSettings.Model;
                analysis.PromptVersion = "v1";

                var usageRecord = new AiUsageRecord
                {
                    Id = Guid.NewGuid(),
                    MarketId = market.Id,
                    OperationType = "Analysis",
                    Model = _openAiSettings.Model,
                    PromptTokens = analysisResult.PromptTokens,
                    CompletionTokens = analysisResult.CompletionTokens,
                    TotalTokens = analysisResult.TotalTokens,
                    EstimatedCostUsd = ((decimal)analysisResult.PromptTokens / 1000000m * 10m) + ((decimal)analysisResult.CompletionTokens / 1000000m * 30m),
                    LatencyMs = stopwatch.ElapsedMilliseconds,
                    IsSuccess = true,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                };

                _dbContext.Set<AiAnalysis>().Add(analysis);
                _dbContext.Set<AiUsageRecord>().Add(usageRecord);
                await _dbContext.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation("AI analysis completed successfully for market {MarketId}. Edge: {Edge}%, Confidence: {Confidence}%, Cost: ${Cost}, Latency: {LatencyMs}ms", 
                    market.Id, analysis.Edge, analysis.Confidence, usageRecord.EstimatedCostUsd, usageRecord.LatencyMs);
            }
            else
            {
                throw new InvalidOperationException("OpenAI API JSON deserialization returned null.");
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var usageRecord = new AiUsageRecord
            {
                Id = Guid.NewGuid(),
                MarketId = market.Id,
                OperationType = "Analysis",
                Model = _openAiSettings.Model,
                IsSuccess = false,
                LatencyMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = ex.Message,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            _dbContext.Set<AiUsageRecord>().Add(usageRecord);
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }
}
