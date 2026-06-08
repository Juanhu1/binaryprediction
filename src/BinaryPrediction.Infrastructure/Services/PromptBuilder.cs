using BinaryPrediction.Core.Entities;

namespace BinaryPrediction.Infrastructure.Services;

public static class PromptBuilder
{
    public static string BuildAnalysisPrompt(Market market)
    {
        return $@"
Analyze the following binary prediction market objectively.

Market Question: {market.Question}
Current Market Probability: {market.Probability}%
End Date: {market.EndDate:O}

Provide your analysis in STRICT JSON format with NO markdown formatting, NO extra text.
The JSON must adhere to the following structure:
{{
  ""estimatedProbability"": <integer between 0 and 100>,
  ""confidence"": <integer between 0 and 100>,
  ""summary"": ""<short summary of your reasoning>"",
  ""keyReasons"": [ ""<reason 1>"", ""<reason 2>"" ],
  ""riskFactors"": [ ""<risk 1>"", ""<risk 2>"" ]
}}
";
    }

    public static string BuildPredictionPrompt(Market market, AiAnalysis analysis)
    {
        return $@"
Based on the following market data and AI analysis, generate a final prediction.

Market Question: {market.Question}
Current Market Probability: {market.Probability}%
End Date: {market.EndDate:O}

Previous Analysis Summary: {analysis.Summary}
Calculated Edge: {analysis.Edge}%
Analysis Confidence: {analysis.Confidence}%

Provide your prediction in STRICT JSON format with NO markdown formatting, NO extra text.
The JSON must adhere to the following structure:
{{
  ""predictedOutcome"": ""<Yes or No>"",
  ""confidenceScore"": <integer between 0 and 100>,
  ""reasoningSummary"": ""<concise explanation>""
}}
";
    }
}
