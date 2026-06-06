using BinaryPrediction.Core.Enums;
using BinaryPrediction.Infrastructure.Services.Classification;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BinaryPrediction.Core.Tests;

public class MarketCategoryResolverTests
{
    private readonly MarketCategoryResolver _resolver;

    public MarketCategoryResolverTests()
    {
        var logger = NullLogger<SportsClassifier>.Instance;
        var sportsClassifier = new SportsClassifier(logger);
        _resolver = new MarketCategoryResolver(sportsClassifier);
    }

    [Theory]
    // Previous tests
    [InlineData("Will Brazil win the 2026 FIFA World Cup?", "Sports")]
    [InlineData("Will the Vegas Golden Knights win the 2026 NHL Stanley Cup?", "Sports")]
    [InlineData("Will Oklahoma City Thunder win the 2026 NBA Finals?", "Sports")]
    [InlineData("Will Bitcoin reach $100k by December?", "Crypto")]
    [InlineData("Who will win the 2024 Presidential Election?", "Politics")]
    [InlineData("Is artificial intelligence going to take over jobs?", "Technology")]
    [InlineData("Will the fed rate drop?", "Economics")]
    [InlineData("Doge coin to the moon!", "Meme")]
    [InlineData("Random event happening somewhere in the world", "Other")]
    // New Team-specific tests
    [InlineData("Will the Cavaliers make the playoffs?", "Sports")]
    [InlineData("Will the Cleveland Cavaliers win the 2026 NBA Finals?", "Sports")]
    [InlineData("Will the Thunder win tonight?", "Sports")]
    [InlineData("Can the Canadiens bounce back this season?", "Sports")]
    [InlineData("Are the Lakers trading LeBron?", "Sports")]
    [InlineData("Golden Knights next game prediction", "Sports")]
    public void Classify_ByQuestionText_ReturnsCorrectCategory(string question, string expectedCategoryName)
    {
        var expectedCategory = Enum.Parse<MarketCategory>(expectedCategoryName);

        var result = _resolver.Classify(question, null);

        Assert.Equal(expectedCategory, result);
    }

    [Theory]
    [InlineData(new[] { "Crypto" }, "Crypto")]
    [InlineData(new[] { "Politics", "Election" }, "Politics")]
    [InlineData(new[] { "NFL" }, "Sports")]
    [InlineData(new[] { "Entertainment" }, "Entertainment")]
    public void Classify_ByTags_ReturnsCorrectCategory(string[] tags, string expectedCategoryName)
    {
        var expectedCategory = Enum.Parse<MarketCategory>(expectedCategoryName);

        var result = _resolver.Classify("Unrelated generic question text", tags);

        Assert.Equal(expectedCategory, result);
    }
}
