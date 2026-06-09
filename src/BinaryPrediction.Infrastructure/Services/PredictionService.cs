using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace BinaryPrediction.Infrastructure.Services;

public class PredictionService : IPredictionService
{
    private readonly IOpenAiAnalysisService _openAiService;
    private readonly IPredictionRepository _predictionRepository;
    private readonly ILogger<PredictionService> _logger;
    private readonly BinaryPrediction.Infrastructure.Persistence.BinaryPredictionDbContext _dbContext;
    private readonly BinaryPrediction.Core.Common.OpenAiSettings _openAiSettings;

    public PredictionService(
        IOpenAiAnalysisService openAiService,
        IPredictionRepository predictionRepository,
        ILogger<PredictionService> logger,
        BinaryPrediction.Infrastructure.Persistence.BinaryPredictionDbContext dbContext,
        Microsoft.Extensions.Options.IOptions<BinaryPrediction.Core.Common.OpenAiSettings> openAiOptions)
    {
        _openAiService = openAiService;
        _predictionRepository = predictionRepository;
        _logger = logger;
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

        _logger.LogInformation("Generating prediction for Analysis {AnalysisId}, Market {MarketId}", analysis.Id, market.Id);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var predictionDto = await _openAiService.GeneratePredictionAsync(market, analysis, cancellationToken);
            stopwatch.Stop();

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
                PromptVersionUsed = "v1",
                IsActive = true
            };

            var usageRecord = new AiUsageRecord
            {
                Id = Guid.NewGuid(),
                MarketId = market.Id,
                OperationType = "Prediction",
                Model = _openAiSettings.Model,
                PromptTokens = predictionDto.PromptTokens,
                CompletionTokens = predictionDto.CompletionTokens,
                TotalTokens = predictionDto.TotalTokens,
                EstimatedCostUsd = ((decimal)predictionDto.PromptTokens / 1000000m * 10m) + ((decimal)predictionDto.CompletionTokens / 1000000m * 30m),
                LatencyMs = stopwatch.ElapsedMilliseconds,
                IsSuccess = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            await _predictionRepository.AddAsync(prediction, cancellationToken);
            _dbContext.Set<AiUsageRecord>().Add(usageRecord);
            await _predictionRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Prediction {PredictionId} created and activated for Analysis {AnalysisId}, Market {MarketId} with Outcome '{Outcome}' and Confidence {Confidence}. Cost: ${Cost}, Latency: {LatencyMs}ms", 
                prediction.Id, analysis.Id, market.Id, prediction.PredictedOutcome, prediction.ConfidencePercentage, usageRecord.EstimatedCostUsd, usageRecord.LatencyMs);

            return prediction;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var usageRecord = new AiUsageRecord
            {
                Id = Guid.NewGuid(),
                MarketId = market.Id,
                OperationType = "Prediction",
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

    public async Task<IEnumerable<Prediction>> GetActivePredictionsAsync(CancellationToken cancellationToken = default)
    {
        return await _predictionRepository.GetActiveAsync(cancellationToken);
    }

    public async Task<Prediction?> GetLatestPredictionAsync(Guid marketId, CancellationToken cancellationToken = default)
    {
        return await _predictionRepository.GetLatestByMarketIdAsync(marketId, cancellationToken);
    }
}
