using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace BinaryPrediction.Infrastructure.Services;

public class PredictionService : IPredictionService
{
    private readonly IOpenAiAnalysisService _openAiService;
    private readonly IPredictionRepository _predictionRepository;
    private readonly ILogger<PredictionService> _logger;

    public PredictionService(
        IOpenAiAnalysisService openAiService,
        IPredictionRepository predictionRepository,
        ILogger<PredictionService> logger)
    {
        _openAiService = openAiService;
        _predictionRepository = predictionRepository;
        _logger = logger;
    }

    public async Task<Prediction?> CreatePredictionAsync(AiAnalysis analysis, Market market, CancellationToken cancellationToken = default)
    {
        var existingPrediction = await _predictionRepository.GetLatestByMarketIdAsync(market.Id, cancellationToken);
        if (existingPrediction != null)
        {
            _logger.LogInformation("Prediction already exists for market {MarketId}", market.Id);
            return null; // Skip generation
        }

        _logger.LogInformation("Generating prediction for Analysis {AnalysisId}, Market {MarketId}", analysis.Id, market.Id);

        var predictionDto = await _openAiService.GeneratePredictionAsync(market, analysis, cancellationToken);

        if (predictionDto == null)
        {
            throw new InvalidOperationException("OpenAI API returned null for Prediction generation.");
        }

        if (predictionDto.ConfidencePercentage < 50m || predictionDto.ConfidencePercentage > 100m)
        {
            _logger.LogWarning("Invalid confidence percentage {ConfidencePercentage} returned for market {MarketId}. Must be between 50 and 100. Skipping prediction creation.", predictionDto.ConfidencePercentage, market.Id);
            return null;
        }

        var prediction = new Prediction
        {
            MarketId = market.Id,
            AnalysisId = analysis.Id,
            PredictedOutcome = predictionDto.PredictedOutcome,
            ConfidencePercentage = predictionDto.ConfidencePercentage,
            ReasoningSummary = predictionDto.ReasoningSummary,
            IsActive = true
        };

        await _predictionRepository.AddAsync(prediction, cancellationToken);
        await _predictionRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Prediction {PredictionId} created and activated for Market {MarketId} with Outcome '{Outcome}' and Confidence {Confidence}", 
            prediction.Id, market.Id, prediction.PredictedOutcome, prediction.ConfidencePercentage);

        return prediction;
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
