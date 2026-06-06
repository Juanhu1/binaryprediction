using BinaryPrediction.Infrastructure.External.Polymarket.DTOs;
using BinaryPrediction.Infrastructure.Services;
using Xunit;

namespace BinaryPrediction.Core.Tests;

public class MarketResolutionDateResolverTests
{
    private readonly MarketResolutionDateResolver _resolver;

    public MarketResolutionDateResolverTests()
    {
        _resolver = new MarketResolutionDateResolver();
    }

    [Fact]
    public void ResolveDate_WithExplicitEndDate_ReturnsEndDate()
    {
        var expected = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var (date, method) = _resolver.ResolveDate(null, expected, DateTimeOffset.UtcNow);

        Assert.Equal(expected, date);
        Assert.Contains("explicit", method);
    }

    [Fact]
    public void ResolveDate_WithAlternativeDate_ReturnsAlternativeDate()
    {
        var expected = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var (date, method) = _resolver.ResolveDate(null, null, expected);

        Assert.Equal(expected, date);
        Assert.Contains("alternative", method);
    }

    [Theory]
    [InlineData("Will Brazil win the 2026 FIFA World Cup?", 2026, 7, 15)]
    [InlineData("Will the Vegas Golden Knights win the 2026 NHL Stanley Cup?", 2026, 6, 30)]
    [InlineData("Will Oklahoma City Thunder win the 2026 NBA Finals?", 2026, 6, 30)]
    [InlineData("Who wins the 2025 Super Bowl?", 2025, 2, 28)]
    public void ResolveDate_WithSportsQuestion_InfersCorrectDate(string question, int year, int month, int day)
    {
        var expected = new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero);

        var (date, method) = _resolver.ResolveDate(question, null, null);

        Assert.Equal(expected, date);
        Assert.Contains("inferred", method);
    }

    [Fact]
    public void ResolveDate_WithUnknownQuestion_ReturnsNull()
    {
        var (date, method) = _resolver.ResolveDate("Will some random thing happen in 2025?", null, null);

        Assert.Null(date);
        Assert.Contains("could not be determined", method);
    }
}
