using System.Text.RegularExpressions;
using BinaryPrediction.Core.Interfaces;

namespace BinaryPrediction.Infrastructure.Services;

public partial class MarketQuestionNormalizer : IMarketQuestionNormalizer
{
    public string Normalize(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
            return string.Empty;

        // Replace newlines and tabs with space
        var normalized = question.Replace("\n", " ").Replace("\r", " ").Replace("\t", " ");

        // Remove multiple spaces using compiled regex
        normalized = MultipleSpacesRegex().Replace(normalized, " ");

        return normalized.Trim();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultipleSpacesRegex();
}
