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
You are an expert forecasting analyst. Based on the following market data and AI analysis, estimate the event probability and generate a forecast.

Market Question: {market.Question}
Current Market Probability: {market.Probability}%
End Date: {market.EndDate:O}

Previous Analysis Summary: {analysis.Summary}
Calculated Edge: {analysis.Edge}%
Analysis Confidence: {analysis.Confidence}%

Instructions:
1. Estimate the probability that the event actually occurs (Event Probability) as a calibrated probability between 0 and 100.
2. Do NOT derive probability from confidence. Think like a forecasting analyst. Consider base rates, historical data, competition, uncertainty, and market context.
3. Determine predictedOutcome: ""Yes"" if eventProbability >= 50, otherwise ""No"".
4. Estimate your confidence in this forecast's quality and reasoning between 0 and 100 (where 0 means no confidence and 100 means absolute confidence).
5. Explicitly distinguish between Event Probability (chance of event occurring) and Confidence (your self-assessed forecast quality and reasoning strength).
   - Example of Bad: Event Probability = 75 because confidence is 75 (this conflates event probability with confidence in forecast).
   - Example of Good: Event Probability = 12, Confidence = 88 (low probability event, but highly confident in that assessment).

Provide your prediction in STRICT JSON format with NO markdown formatting, NO extra text.
The JSON must adhere to the following structure:
{
  ""eventProbability"": <integer between 0 and 100>,
  ""predictedOutcome"": ""<Yes or No>"",
  ""confidence"": <integer between 0 and 100>,
  ""reasoning"": ""<concise explanation>""
}
";
    }
}
