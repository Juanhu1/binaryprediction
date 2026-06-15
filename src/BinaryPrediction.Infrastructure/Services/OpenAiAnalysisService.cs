using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BinaryPrediction.Core.Common;
using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BinaryPrediction.Infrastructure.Services;

public class OpenAiAnalysisService : IOpenAiAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiSettings _settings;
    private readonly ILogger<OpenAiAnalysisService> _logger;
    private readonly IMockAnalysisGenerator _mockAnalysisGenerator;
    private readonly IMockPredictionGenerator _mockPredictionGenerator;
    private readonly IOpenAiRetryService _retryService;
    private readonly IPromptService _promptService;

    public OpenAiAnalysisService(
        HttpClient httpClient, 
        IOptions<OpenAiSettings> options,
        ILogger<OpenAiAnalysisService> logger,
        IMockAnalysisGenerator mockAnalysisGenerator,
        IMockPredictionGenerator mockPredictionGenerator,
        IOpenAiRetryService retryService,
        IPromptService promptService)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;
        _mockAnalysisGenerator = mockAnalysisGenerator;
        _mockPredictionGenerator = mockPredictionGenerator;
        _retryService = retryService;
        _promptService = promptService;

        _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
    }

    public async Task<AiAnalysisResultDto?> AnalyzeMarketAsync(Market market, CancellationToken cancellationToken = default)
    {
        if (_settings.UseMockAnalysis)
        {
            _logger.LogInformation("Mock Analysis generated for market {MarketId}", market.Id);
            return _mockAnalysisGenerator.Generate(market);
        }

        _logger.LogInformation("Generating real AI analysis for market {MarketId}", market.Id);

        var prompt = await _promptService.GetAnalysisPromptAsync(market, cancellationToken);

        var requestBody = new
        {
            model = _settings.Model,
            messages = new[]
            {
                new { role = "system", content = "You are an expert prediction market analyst." },
                new { role = "user", content = prompt }
            },
            temperature = _settings.Temperature,
            response_format = new { type = "json_object" }
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        return await _retryService.ExecuteAsync(async ct =>
        {
            var response = await _httpClient.PostAsync("chat/completions", content, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("OpenAI API call failed. Endpoint: {Endpoint}, Model: {Model}, Status: {StatusCode}, Body: {Error}", 
                    response.RequestMessage?.RequestUri, _settings.Model, response.StatusCode, errorBody);
                
                throw new InvalidOperationException($"OpenAI API HTTP {(int)response.StatusCode} - {response.ReasonPhrase}: {errorBody}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync(ct);
            var resultDocument = JsonDocument.Parse(jsonResponse);
            
            var contentString = resultDocument.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(contentString))
            {
                throw new InvalidOperationException("OpenAI API returned empty content string.");
            }

            var result = JsonSerializer.Deserialize<AiAnalysisResultDto>(contentString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (result == null) throw new InvalidOperationException("Failed to deserialize analysis JSON.");

            if (resultDocument.RootElement.TryGetProperty("usage", out var usageProp))
            {
                result.PromptTokens = usageProp.TryGetProperty("prompt_tokens", out var p) ? p.GetInt32() : 0;
                result.CompletionTokens = usageProp.TryGetProperty("completion_tokens", out var c) ? c.GetInt32() : 0;
                result.TotalTokens = usageProp.TryGetProperty("total_tokens", out var t) ? t.GetInt32() : 0;
            }

            return result;
        }, cancellationToken);
    }

    public async Task<AiPredictionResultDto?> GeneratePredictionAsync(Market market, AiAnalysis analysis, CancellationToken cancellationToken = default)
    {
        if (_settings.UseMockPrediction)
        {
            _logger.LogInformation("Mock Prediction generated for market {MarketId}", market.Id);
            return _mockPredictionGenerator.Generate(market, analysis);
        }

        _logger.LogInformation("Generating real AI prediction for market {MarketId}", market.Id);

        var prompt = await _promptService.GetPredictionPromptAsync(market, analysis, cancellationToken);

        var requestBody = new
        {
            model = _settings.Model,
            messages = new[]
            {
                new { role = "system", content = "You are an expert prediction market analyst." },
                new { role = "user", content = prompt }
            },
            temperature = _settings.Temperature,
            response_format = new { type = "json_object" }
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        return await _retryService.ExecuteAsync(async ct =>
        {
            var response = await _httpClient.PostAsync("chat/completions", content, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("OpenAI API call failed for Prediction. Endpoint: {Endpoint}, Model: {Model}, Status: {StatusCode}, Body: {Error}", 
                    response.RequestMessage?.RequestUri, _settings.Model, response.StatusCode, errorBody);
                    
                throw new InvalidOperationException($"OpenAI API HTTP {(int)response.StatusCode} - {response.ReasonPhrase}: {errorBody}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync(ct);
            var resultDocument = JsonDocument.Parse(jsonResponse);
            
            var contentString = resultDocument.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(contentString))
            {
                throw new InvalidOperationException("OpenAI API returned empty content string for Prediction.");
            }

            var result = JsonSerializer.Deserialize<AiPredictionResultDto>(contentString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (result == null) throw new InvalidOperationException("Failed to deserialize prediction JSON.");

            result.PromptVersionUsed = "v2";

            // Parse response dynamically to support legacy JSON properties from DB-configured templates
            using (var doc = JsonDocument.Parse(contentString))
            {
                var root = doc.RootElement;
                if (root.TryGetProperty("confidencePercentage", out var legacyConfProp) && result.ConfidencePercentage == 0)
                {
                    result.ConfidencePercentage = legacyConfProp.GetDecimal();
                }
                if (root.TryGetProperty("reasoningSummary", out var legacyReasonProp) && string.IsNullOrEmpty(result.ReasoningSummary))
                {
                    result.ReasoningSummary = legacyReasonProp.GetString() ?? string.Empty;
                }

                // Check case-insensitively if "eventProbability" or "event_probability" exists and is not null/undefined
                bool hasEventProbability = false;
                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.Name.Equals("eventProbability", StringComparison.OrdinalIgnoreCase) ||
                        prop.Name.Equals("event_probability", StringComparison.OrdinalIgnoreCase))
                    {
                        if (prop.Value.ValueKind != JsonValueKind.Null && prop.Value.ValueKind != JsonValueKind.Undefined)
                        {
                            hasEventProbability = true;
                        }
                        break;
                    }
                }

                if (!hasEventProbability)
                {
                    result.EventProbability = result.PredictedOutcome.Equals("Yes", StringComparison.OrdinalIgnoreCase)
                        ? result.ConfidencePercentage
                        : 100m - result.ConfidencePercentage;
                    result.PromptVersionUsed = "v1";
                }
            }

            // Force PredictedOutcome from EventProbability >= 50, not from the model's returned predictedOutcome
            result.PredictedOutcome = result.EventProbability >= 50m ? "Yes" : "No";

            // Strict validation rules
            if (result.EventProbability < 0 || result.EventProbability > 100)
            {
                throw new InvalidOperationException($"Invalid event probability: {result.EventProbability}. Must be between 0 and 100.");
            }

            if (result.ConfidencePercentage < 0 || result.ConfidencePercentage > 100)
            {
                throw new InvalidOperationException($"Invalid confidence: {result.ConfidencePercentage}. Must be between 0 and 100.");
            }

            if (result.PredictedOutcome != "Yes" && result.PredictedOutcome != "No")
            {
                throw new InvalidOperationException($"Invalid outcome: '{result.PredictedOutcome}'. Must be 'Yes' or 'No'.");
            }

            if (resultDocument.RootElement.TryGetProperty("usage", out var usageProp))
            {
                result.PromptTokens = usageProp.TryGetProperty("prompt_tokens", out var p) ? p.GetInt32() : 0;
                result.CompletionTokens = usageProp.TryGetProperty("completion_tokens", out var c) ? c.GetInt32() : 0;
                result.TotalTokens = usageProp.TryGetProperty("total_tokens", out var t) ? t.GetInt32() : 0;
            }

            _logger.LogInformation("[PIPELINE_TRACE] OpenAiAnalysisService.GeneratePredictionAsync: AnalysisId={AnalysisId}, EventProbability={EventProbability}, ConfidencePercentage={ConfidencePercentage}, PredictedOutcome={PredictedOutcome}, PromptVersionUsed={PromptVersionUsed}",
                analysis.Id, result.EventProbability, result.ConfidencePercentage, result.PredictedOutcome, result.PromptVersionUsed);

            return result;
        }, cancellationToken);
    }
}
