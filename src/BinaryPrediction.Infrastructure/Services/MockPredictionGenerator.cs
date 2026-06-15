using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;

namespace BinaryPrediction.Infrastructure.Services;

public class MockPredictionGenerator : IMockPredictionGenerator
{
    private readonly Random _random = new();

    public AiPredictionResultDto Generate(Market market, AiAnalysis analysis)
    {
        // Estimated probability mapped from analysis
        var eventProbability = analysis.EstimatedProbability;
        var outcome = eventProbability >= 50 ? "Yes" : "No";

        // Vary confidence slightly from the analysis confidence
        var variation = _random.Next(-5, 6);
        var confidence = Math.Clamp(analysis.Confidence + variation, 0, 100);

        var reasoningVariations = new[]
        {
            $"Based on the calculated {eventProbability}% edge, the simulated system predicts {outcome}.",
            $"The market '{market.Question}' shows strong indicators for {outcome} with a baseline confidence of {analysis.Confidence}%.",
            $"After reviewing the mock analysis factors, {outcome} is the most logical conclusion."
        };

        var reasoning = reasoningVariations[_random.Next(reasoningVariations.Length)];

        return new AiPredictionResultDto
        {
            PredictedOutcome = outcome,
            ConfidencePercentage = confidence,
            ReasoningSummary = reasoning,
            EventProbability = eventProbability
        };
    }
}
