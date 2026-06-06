using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Enums;
using BinaryPrediction.Core.Interfaces;

namespace BinaryPrediction.Infrastructure.Services;

public class MarketQualityScoringService : IMarketQualityScoringService
{
    private readonly IMarketCategoryClassifier _classifier;

    public MarketQualityScoringService(IMarketCategoryClassifier classifier)
    {
        _classifier = classifier;
    }

    public (int Score, MarketCategory Category, string? ImmediateRejectionReason) EvaluateMarketQuality(
        string question, 
        decimal liquidity, 
        decimal volume, 
        IReadOnlyList<string>? tags)
    {
        var category = _classifier.Classify(question, tags);
        
        if (category == MarketCategory.Meme)
        {
            return (0, category, "Market categorized as Meme.");
        }

        int score = 50; // Base score

        // Liquidity bonuses/penalties
        if (liquidity > 50000m) score += 20;
        else if (liquidity > 10000m) score += 10;
        else if (liquidity < 1000m) score -= 20;
        else if (liquidity < 100m) score -= 40;

        // Volume bonuses
        if (volume > 100000m) score += 20;
        else if (volume > 20000m) score += 10;

        // Word count penalty/bonus (proxy for descriptive wording)
        var wordCount = question.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount < 4) score -= 20; // Ambiguous or too short
        else if (wordCount > 10) score += 10; // Clear wording

        // Clamp between 0 and 100
        score = Math.Clamp(score, 0, 100);

        return (score, category, null);
    }
}
