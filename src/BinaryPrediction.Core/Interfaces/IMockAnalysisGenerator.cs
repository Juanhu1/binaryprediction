using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Entities;

namespace BinaryPrediction.Core.Interfaces;

public interface IMockAnalysisGenerator
{
    AiAnalysisResultDto Generate(Market market);
}
