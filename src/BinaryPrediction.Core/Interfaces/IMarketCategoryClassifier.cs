using BinaryPrediction.Core.Enums;

namespace BinaryPrediction.Core.Interfaces;

public interface IMarketCategoryClassifier
{
    MarketCategory Classify(string question, IReadOnlyList<string>? tags);
}
