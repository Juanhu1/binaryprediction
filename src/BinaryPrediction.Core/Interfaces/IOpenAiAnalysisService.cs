using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Entities;

namespace BinaryPrediction.Core.Interfaces;

public interface IOpenAiAnalysisService
{
    Task<AiAnalysisResultDto?> AnalyzeMarketAsync(Market market, CancellationToken cancellationToken = default);
}
