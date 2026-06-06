using BinaryPrediction.Core.Entities;

namespace BinaryPrediction.Core.Interfaces;

public interface IMarketEligibilityService
{
    bool EvaluateEligibility(Market market, out string? reason);
}
