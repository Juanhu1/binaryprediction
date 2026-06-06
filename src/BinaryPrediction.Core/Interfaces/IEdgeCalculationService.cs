using BinaryPrediction.Core.Entities;

namespace BinaryPrediction.Core.Interfaces;

public interface IEdgeCalculationService
{
    AiAnalysis CalculateEdge(Market market, decimal estimatedProbability, decimal confidence);
}
