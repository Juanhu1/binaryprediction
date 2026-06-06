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

        var analysisResult = await _openAiService.AnalyzeMarketAsync(market, cancellationToken);

        if (analysisResult != null)
        {
            var analysis = _edgeCalculationService.CalculateEdge(
                market, 
                analysisResult.EstimatedProbability, 
                analysisResult.Confidence);

            analysis.Summary = analysisResult.Summary;
            analysis.KeyReasonsJson = JsonSerializer.Serialize(analysisResult.KeyReasons);
            analysis.RiskFactorsJson = JsonSerializer.Serialize(analysisResult.RiskFactors);
            
            analysis.ModelName = _openAiSettings.Model;
            analysis.PromptVersion = "v1";

            _dbContext.Set<AiAnalysis>().Add(analysis);
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("AI analysis completed successfully for market {MarketId}. Edge: {Edge}%, Confidence: {Confidence}%", 
                market.Id, analysis.Edge, analysis.Confidence);
        }
        else
        {
            throw new InvalidOperationException("OpenAI API JSON deserialization returned null.");
        }
    }
}
