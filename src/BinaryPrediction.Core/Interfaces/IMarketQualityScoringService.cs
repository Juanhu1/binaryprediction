using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Enums;

namespace BinaryPrediction.Core.Interfaces;

public interface IMarketQualityScoringService
{
    (int Score, MarketCategory Category, string? ImmediateRejectionReason) EvaluateMarketQuality(
        string question, 
        decimal liquidity, 
        decimal volume, 
        IReadOnlyList<string>? tags);
}
