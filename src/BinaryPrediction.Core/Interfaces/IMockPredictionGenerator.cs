using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Entities;

namespace BinaryPrediction.Core.Interfaces;

public interface IMockPredictionGenerator
{
    AiPredictionResultDto Generate(Market market, AiAnalysis analysis);
}
