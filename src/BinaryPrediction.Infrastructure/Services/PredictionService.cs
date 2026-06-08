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

    public async Task<Prediction> CreatePredictionAsync(AiAnalysis analysis, Market market, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating prediction for Analysis {AnalysisId}, Market {MarketId}", analysis.Id, market.Id);

        var predictionDto = await _openAiService.GeneratePredictionAsync(market, analysis, cancellationToken);

        if (predictionDto == null)
        {
            throw new InvalidOperationException("OpenAI API returned null for Prediction generation.");
        }

        var prediction = new Prediction
        {
            MarketId = market.Id,
            AnalysisId = analysis.Id,
            PredictedOutcome = predictionDto.PredictedOutcome,
            ConfidenceScore = Math.Clamp(predictionDto.ConfidenceScore, 0m, 100m),
            ReasoningSummary = predictionDto.ReasoningSummary,
            IsActive = true
        };

        // Deactivate older predictions for the same market
        await _predictionRepository.DeactivatePreviousPredictionsAsync(market.Id, cancellationToken);

        await _predictionRepository.AddAsync(prediction, cancellationToken);
        await _predictionRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Prediction {PredictionId} created and activated for Market {MarketId} with Outcome '{Outcome}' and Confidence {Confidence}", 
            prediction.Id, market.Id, prediction.PredictedOutcome, prediction.ConfidenceScore);

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
