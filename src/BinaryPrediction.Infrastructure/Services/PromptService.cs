using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BinaryPrediction.Infrastructure.Services;

public class PromptService : IPromptService
{
    private readonly BinaryPredictionDbContext _dbContext;
    private readonly ILogger<PromptService> _logger;

    public PromptService(BinaryPredictionDbContext dbContext, ILogger<PromptService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<string> GetAnalysisPromptAsync(Market market, CancellationToken cancellationToken = default)
    {
        var version = await _dbContext.PromptVersions
            .Where(p => p.PromptName == "AnalysisPrompt")
            .OrderByDescending(p => p.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        string template = version?.PromptTemplate ?? GetDefaultAnalysisPrompt();

        return template
            .Replace("{market.Question}", market.Question)
            .Replace("{market.Probability}", market.Probability.ToString())
            .Replace("{market.EndDate:O}", market.EndDate?.ToString("O") ?? "N/A");
    }

    public async Task<string> GetPredictionPromptAsync(Market market, AiAnalysis analysis, CancellationToken cancellationToken = default)
    {
        var version = await _dbContext.PromptVersions
            .Where(p => p.PromptName == "PredictionPrompt")
            .OrderByDescending(p => p.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        string template = version?.PromptTemplate ?? GetDefaultPredictionPrompt();

        return template
            .Replace("{market.Question}", market.Question)
            .Replace("{market.Probability}", market.Probability.ToString())
            .Replace("{market.EndDate:O}", market.EndDate?.ToString("O") ?? "N/A")
            .Replace("{analysis.Summary}", analysis.Summary)
            .Replace("{analysis.Edge}", analysis.Edge.ToString())
            .Replace("{analysis.Confidence}", analysis.Confidence.ToString());
    }

    private string GetDefaultAnalysisPrompt()
    {
        return @"
Analyze the following binary prediction market objectively.

Market Question: {market.Question}
Current Market Probability: {market.Probability}%
End Date: {market.EndDate:O}

Provide your analysis in STRICT JSON format with NO markdown formatting, NO extra text.
The JSON must adhere to the following structure:
{
  ""estimatedProbability"": <integer between 0 and 100>,
  ""confidence"": <integer between 0 and 100>,
  ""summary"": ""<short summary of your reasoning>"",
  ""keyReasons"": [ ""<reason 1>"", ""<reason 2>"" ],
  ""riskFactors"": [ ""<risk 1>"", ""<risk 2>"" ]
}
";
    }

    private string GetDefaultPredictionPrompt()
    {
        return @"
Based on the following market data and AI analysis, generate a final prediction.

Market Question: {market.Question}
Current Market Probability: {market.Probability}%
End Date: {market.EndDate:O}

Previous Analysis Summary: {analysis.Summary}
Calculated Edge: {analysis.Edge}%
Analysis Confidence: {analysis.Confidence}%

Provide your prediction in STRICT JSON format with NO markdown formatting, NO extra text.
The JSON must adhere to the following structure:
{
  ""predictedOutcome"": ""<Yes or No>"",
  ""confidencePercentage"": <integer between 50 and 100>,
  ""reasoningSummary"": ""<concise explanation>""
}

Rules:
- confidencePercentage must be between 50 and 100.
- 50 = highly uncertain, 100 = near certainty.
- Never return values below 50.
";
    }
}
