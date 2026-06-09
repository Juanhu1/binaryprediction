using BinaryPrediction.Core.Entities;

namespace BinaryPrediction.Core.Interfaces;

public interface IPromptService
{
    Task<string> GetAnalysisPromptAsync(Market market, CancellationToken cancellationToken = default);
    Task<string> GetPredictionPromptAsync(Market market, AiAnalysis analysis, CancellationToken cancellationToken = default);
}
