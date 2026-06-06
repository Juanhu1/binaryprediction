using Microsoft.Extensions.Logging;

namespace BinaryPrediction.Infrastructure.Services.Classification;

public class SportsClassifier
{
    private readonly ILogger<SportsClassifier> _logger;

    public SportsClassifier(ILogger<SportsClassifier> logger)
    {
        _logger = logger;
    }

    public (bool IsSports, string? MatchReason) Classify(string text, IReadOnlyList<string> tags)
    {
        bool HasTag(string tagMatch) => tags.Any(t => t.Contains(tagMatch, StringComparison.OrdinalIgnoreCase));

        // 1. Base keywords and leagues
        foreach (var keyword in SportsKeywordRegistry.BaseKeywords)
        {
            if (HasTag(keyword) || text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Sports category inferred from base keyword: {Keyword}", keyword);
                return (true, $"base keyword: {keyword}");
            }
        }

        foreach (var league in SportsKeywordRegistry.Leagues)
        {
            if (HasTag(league) || text.Contains(league, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Sports category inferred from league keyword: {League}", league);
                return (true, $"league keyword: {league}");
            }
        }

        // 2. Team Dictionaries
        if (CheckTeam(text, tags, SportsKeywordRegistry.NbaTeams, "NBA", out var reasonNba)) return (true, reasonNba);
        if (CheckTeam(text, tags, SportsKeywordRegistry.NhlTeams, "NHL", out var reasonNhl)) return (true, reasonNhl);
        if (CheckTeam(text, tags, SportsKeywordRegistry.NflTeams, "NFL", out var reasonNfl)) return (true, reasonNfl);
        if (CheckTeam(text, tags, SportsKeywordRegistry.MlbTeams, "MLB", out var reasonMlb)) return (true, reasonMlb);
        if (CheckTeam(text, tags, SportsKeywordRegistry.FifaNationalTeams, "FIFA", out var reasonFifa)) return (true, reasonFifa);

        return (false, null);
    }

    private bool CheckTeam(string text, IReadOnlyList<string> tags, string[] teams, string leagueName, out string? reason)
    {
        foreach (var team in teams)
        {
            if (tags.Any(t => t.Contains(team, StringComparison.OrdinalIgnoreCase)) || text.Contains(team, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Sports category inferred from {League} team keyword: {Team}", leagueName, team);
                reason = $"{leagueName} team keyword: {team}";
                return true;
            }
        }

        reason = null;
        return false;
    }
}
