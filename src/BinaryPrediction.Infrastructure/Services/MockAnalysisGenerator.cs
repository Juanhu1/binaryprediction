using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;

namespace BinaryPrediction.Infrastructure.Services;

public class MockAnalysisGenerator : IMockAnalysisGenerator
{
    private readonly Random _random = new();

    public AiAnalysisResultDto Generate(Market market)
    {
        var probability = _random.Next(40, 76); // 40 to 75
        var confidence = _random.Next(60, 96); // 60 to 95

        return new AiAnalysisResultDto
        {
            EstimatedProbability = probability,
            Confidence = confidence,
            Summary = $"Mock analysis generated for: '{market.Question}'. Evaluated market conditions and calculated an estimated probability of {probability}%.",
            KeyReasons = new List<string> 
            { 
                "Historical mock trends align with this probability.",
                "Simulated data indicates this outcome."
            },
            RiskFactors = new List<string>
            {
                "This is a mock analysis so it lacks real-world edge.",
                "Unpredictable volatility in mock variables."
            }
        };
    }
}
