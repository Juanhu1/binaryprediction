using BinaryPrediction.Core.Enums;
using BinaryPrediction.Core.Interfaces;

namespace BinaryPrediction.Infrastructure.Services.Classification;

public class MarketCategoryResolver : IMarketCategoryClassifier
{
    private readonly SportsClassifier _sportsClassifier;

    public MarketCategoryResolver(SportsClassifier sportsClassifier)
    {
        _sportsClassifier = sportsClassifier;
    }

    public MarketCategory Classify(string question, IReadOnlyList<string>? tags)
    {
        var tagList = tags ?? Array.Empty<string>();
        var text = question.ToLowerInvariant();

        bool HasTag(string tagMatch) => tagList.Any(t => t.Contains(tagMatch, StringComparison.OrdinalIgnoreCase));

        // Note: Specific exclusions check order matters.
        if (HasTag("meme") || text.Contains("doge") || text.Contains("pepe"))
            return MarketCategory.Meme;

        if (HasTag("crypto") || text.Contains("bitcoin") || text.Contains("ethereum") || text.Contains("solana"))
            return MarketCategory.Crypto;

        if (HasTag("politics") || HasTag("election") || text.Contains("election") || text.Contains("president") || text.Contains("trump") || text.Contains("biden"))
            return MarketCategory.Politics;

        // Delegate Sports to dedicated classifier
        var (isSports, _) = _sportsClassifier.Classify(text, tagList);
        if (isSports)
            return MarketCategory.Sports;

        if (HasTag("economics") || text.Contains("inflation") || text.Contains("fed rate") || text.Contains("gdp"))
            return MarketCategory.Economics;

        if (HasTag("technology") || text.Contains("ai ") || text.Contains("openai") || text.Contains("apple") || text.Contains("microsoft") || text.Contains("artificial intelligence"))
            return MarketCategory.Technology;

        if (HasTag("entertainment") || text.Contains("oscars") || text.Contains("movie") || text.Contains("box office"))
            return MarketCategory.Entertainment;

        return MarketCategory.Other;
    }
}
