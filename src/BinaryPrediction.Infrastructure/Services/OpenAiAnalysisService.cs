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

    public OpenAiAnalysisService(
        HttpClient httpClient, 
        IOptions<OpenAiSettings> options,
        ILogger<OpenAiAnalysisService> logger,
        IMockAnalysisGenerator mockAnalysisGenerator,
        IMockPredictionGenerator mockPredictionGenerator)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;
        _mockAnalysisGenerator = mockAnalysisGenerator;
        _mockPredictionGenerator = mockPredictionGenerator;

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

        var prompt = PromptBuilder.BuildAnalysisPrompt(market);

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

        var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("OpenAI API call failed. Endpoint: {Endpoint}, Model: {Model}, Status: {StatusCode}, Body: {Error}", 
                response.RequestMessage?.RequestUri, _settings.Model, response.StatusCode, errorBody);
            
            throw new InvalidOperationException($"OpenAI API HTTP {(int)response.StatusCode} - {response.ReasonPhrase}: {errorBody}");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        
        _logger.LogInformation("OpenAI API success. Endpoint: {Endpoint}, Model: {Model}. Raw JSON response: {RawResponse}",
            response.RequestMessage?.RequestUri, _settings.Model, jsonResponse);

        var resultDocument = JsonDocument.Parse(jsonResponse);
        var contentString = resultDocument.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(contentString))
        {
            _logger.LogError("OpenAI API returned empty content string.");
            throw new InvalidOperationException("OpenAI API returned empty content string.");
        }

        return JsonSerializer.Deserialize<AiAnalysisResultDto>(contentString, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    public async Task<AiPredictionResultDto?> GeneratePredictionAsync(Market market, AiAnalysis analysis, CancellationToken cancellationToken = default)
    {
        if (_settings.UseMockPrediction)
        {
            _logger.LogInformation("Mock Prediction generated for market {MarketId}", market.Id);
            return _mockPredictionGenerator.Generate(market, analysis);
        }

        _logger.LogInformation("Generating real AI prediction for market {MarketId}", market.Id);

        var prompt = PromptBuilder.BuildPredictionPrompt(market, analysis);

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

        var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("OpenAI API call failed for Prediction. Endpoint: {Endpoint}, Model: {Model}, Status: {StatusCode}, Body: {Error}", 
                response.RequestMessage?.RequestUri, _settings.Model, response.StatusCode, errorBody);
                
            throw new InvalidOperationException($"OpenAI API HTTP {(int)response.StatusCode} - {response.ReasonPhrase}: {errorBody}");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        
        _logger.LogInformation("OpenAI API success for Prediction. Endpoint: {Endpoint}, Model: {Model}. Raw JSON response: {RawResponse}",
            response.RequestMessage?.RequestUri, _settings.Model, jsonResponse);

        var resultDocument = JsonDocument.Parse(jsonResponse);
        var contentString = resultDocument.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(contentString))
        {
            _logger.LogError("OpenAI API returned empty content string for Prediction.");
            throw new InvalidOperationException("OpenAI API returned empty content string for Prediction.");
        }

        return JsonSerializer.Deserialize<AiPredictionResultDto>(contentString, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}
