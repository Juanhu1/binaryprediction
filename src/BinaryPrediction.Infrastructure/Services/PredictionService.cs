using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace BinaryPrediction.Infrastructure.Services;

public class PredictionService : IPredictionService
{
    private readonly IOpenAiAnalysisService _openAiService;
    private readonly IPredictionRepository _predictionRepository;
    private readonly ILogger<PredictionService> _logger;

private readonly IEdgeDetectionService _edgeDetectionService;
    private readonly BinaryPrediction.Infrastructure.Persistence.BinaryPredictionDbContext _dbContext;
    private readonly BinaryPrediction.Core.Common.OpenAiSettings _openAiSettings;

    public PredictionService(
        IOpenAiAnalysisService openAiService,
        IPredictionRepository predictionRepository,
        ILogger<PredictionService> logger,
        IEdgeDetectionService edgeDetectionService,
        BinaryPrediction.Infrastructure.Persistence.BinaryPredictionDbContext dbContext,
        Microsoft.Extensions.Options.IOptions<BinaryPrediction.Core.Common.OpenAiSettings> openAiOptions)
    {
        _openAiService = openAiService;
        _predictionRepository = predictionRepository;
        _logger = logger;
        _edgeDetectionService = edgeDetectionService;
        _dbContext = dbContext;
        _openAiSettings = openAiOptions.Value;
    }

    public async Task<Prediction?> CreatePredictionAsync(AiAnalysis analysis, Market market, CancellationToken cancellationToken = default)
    {
        var hasPrediction = await _predictionRepository.HasPredictionForAnalysisAsync(analysis.Id, cancellationToken);
        if (hasPrediction)
        {
            _logger.LogInformation("Prediction already exists for analysis {AnalysisId}", analysis.Id);
            return null; // Skip generation
        }

        _logger.LogInformation("Generating prediction for Analysis {AnalysisId}, Market {MarketId} directly from AIAnalysis values.", analysis.Id, market.Id);

        try
        {
            var prediction = new Prediction
            {
                MarketId = market.Id,
                AnalysisId = analysis.Id,
                PredictedOutcome = analysis.EstimatedProbability >= 50m ? "Yes" : "No",
                ConfidencePercentage = analysis.Confidence,
                ReasoningSummary = analysis.Summary,
                PromptVersionUsed = "v2",
                IsActive = true,
                AiProbability = analysis.EstimatedProbability
            };

            _logger.LogInformation("[PIPELINE_TRACE] PredictionService.CreatePredictionAsync: AnalysisId={AnalysisId}, EventProbability={EventProbability}, ConfidencePercentage={ConfidencePercentage}, PredictedOutcome={PredictedOutcome}, PromptVersionUsed={PromptVersionUsed}",
                prediction.AnalysisId, prediction.AiProbability, prediction.ConfidencePercentage, prediction.PredictedOutcome, prediction.PromptVersionUsed);

            await _predictionRepository.AddAsync(prediction, cancellationToken);
            await _predictionRepository.SaveChangesAsync(cancellationToken);

            var marketProbPct = market.Probability * 100m;
            var gap = Math.Abs(prediction.AiProbability - marketProbPct);

            _logger.LogInformation("Prediction Metrics logged: Market Probability = {MarketProbPct}%, AI Event Probability = {AiProbability}%, Predicted Outcome = {PredictedOutcome}, Forecast Confidence = {Confidence}%, Probability Gap = {ProbabilityGap}%",
                marketProbPct, prediction.AiProbability, prediction.PredictedOutcome, prediction.ConfidencePercentage, gap);

            try
            {
                _logger.LogInformation(
                    "EDGE TEST BEFORE CALL {PredictionId}",
                    prediction.Id);
                await _edgeDetectionService.DetectOpportunityAsync(prediction.Id, cancellationToken);
                _logger.LogInformation(
                    "EDGE TEST AFTER CALL {PredictionId}",
                    prediction.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to perform edge detection for prediction {PredictionId}", prediction.Id);
            }

            _logger.LogInformation("Prediction {PredictionId} created and activated directly from Analysis {AnalysisId}, Market {MarketId} with Outcome '{Outcome}' and Confidence {Confidence}.", 
                prediction.Id, analysis.Id, market.Id, prediction.PredictedOutcome, prediction.ConfidencePercentage);

            return prediction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save prediction created from analysis {AnalysisId}", analysis.Id);
            throw;
        }
    }

    public async Task<IEnumerable<Prediction>> GetActivePredictionsAsync(CancellationToken cancellationToken = default)
    {
        return await _predictionRepository.GetActiveAsync(cancellationToken);
    }

    public async Task<Prediction?> GetLatestPredictionAsync(Guid marketId, CancellationToken cancellationToken = default)
    {
        return await _predictionRepository.GetLatestByMarketIdAsync(marketId, cancellationToken);
    }
}
