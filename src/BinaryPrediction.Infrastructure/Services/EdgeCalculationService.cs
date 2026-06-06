using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;

namespace BinaryPrediction.Infrastructure.Services;

public class EdgeCalculationService : IEdgeCalculationService
{
    public AiAnalysis CalculateEdge(Market market, decimal estimatedProbability, decimal confidence)
    {
        var edge = estimatedProbability - market.Probability;

        return new AiAnalysis
        {
            MarketId = market.Id,
            MarketProbability = market.Probability,
            EstimatedProbability = estimatedProbability,
            Edge = edge,
            Confidence = confidence,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }
}
