using System.Text.RegularExpressions;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.External.Polymarket.DTOs;

namespace BinaryPrediction.Infrastructure.Services;

public partial class MarketResolutionDateResolver : IMarketResolutionDateResolver
{
    private static readonly Regex YearRegex = new(@"\b(202[4-9]|20[3-9]\d)\b", RegexOptions.Compiled);

    public (DateTimeOffset? Date, string ResolutionMethod) ResolveDate(string? question, DateTimeOffset? explicitEndDate, DateTimeOffset? alternativeDate)
    {
        // Priority 1: Explicit market EndDate from API
        if (explicitEndDate.HasValue)
        {
            return (explicitEndDate, "Resolution date resolved from explicit API date.");
        }

        // Priority 2: Alternative API date fields
        if (alternativeDate.HasValue)
        {
            return (alternativeDate, "Resolution date resolved from alternative API date field.");
        }

        // Priority 3: Infer from question text for Sports
        if (!string.IsNullOrWhiteSpace(question))
        {
            var inferred = InferSportsDate(question);
            if (inferred.HasValue)
            {
                return (inferred.Value.Date, $"Resolution date inferred from {inferred.Value.EventName} {inferred.Value.Year} pattern.");
            }
        }

        return (null, "Resolution date could not be determined.");
    }

    private (DateTimeOffset Date, string EventName, int Year)? InferSportsDate(string question)
    {
        var yearMatch = YearRegex.Match(question);
        if (!yearMatch.Success || !int.TryParse(yearMatch.Value, out var year))
        {
            return null;
        }

        var text = question.ToLowerInvariant();

        if (text.Contains("fifa world cup"))
        {
            return (new DateTimeOffset(year, 7, 15, 0, 0, 0, TimeSpan.Zero), "FIFA World Cup", year);
        }
        
        if (text.Contains("nba finals"))
        {
            return (new DateTimeOffset(year, 6, 30, 0, 0, 0, TimeSpan.Zero), "NBA Finals", year);
        }

        if (text.Contains("stanley cup"))
        {
            return (new DateTimeOffset(year, 6, 30, 0, 0, 0, TimeSpan.Zero), "Stanley Cup", year);
        }

        if (text.Contains("super bowl"))
        {
            return (new DateTimeOffset(year, 2, 28, 0, 0, 0, TimeSpan.Zero), "Super Bowl", year);
        }

        if (text.Contains("champions league"))
        {
            return (new DateTimeOffset(year, 6, 15, 0, 0, 0, TimeSpan.Zero), "UEFA Champions League", year);
        }

        if (text.Contains("olympics"))
        {
            return (new DateTimeOffset(year, 8, 31, 0, 0, 0, TimeSpan.Zero), "Olympics", year);
        }

        if (text.Contains("world series"))
        {
            return (new DateTimeOffset(year, 11, 15, 0, 0, 0, TimeSpan.Zero), "MLB World Series", year);
        }

        return null;
    }
}
