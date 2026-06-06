namespace BinaryPrediction.Infrastructure.Services.Classification;

public static class SportsKeywordRegistry
{
    public static readonly string[] BaseKeywords =
    {
        "sports", "win the match", "super bowl", "finals", "champions league", "world cup", "stanley cup"
    };

    public static readonly string[] Leagues =
    {
        "nfl", "nba", "nhl", "mlb", "uefa", "fifa", "premier league", "la liga"
    };

    public static readonly string[] NbaTeams =
    {
        "Cavaliers", "Thunder", "Lakers", "Warriors", "Celtics", "Bulls", "Knicks",
        "Heat", "Nuggets", "Mavericks", "Suns", "Clippers", "76ers", "Bucks", "Timberwolves"
    };

    public static readonly string[] NhlTeams =
    {
        "Golden Knights", "Canadiens", "Avalanche", "Hurricanes", "Maple Leafs",
        "Rangers", "Bruins", "Oilers", "Lightning", "Panthers"
    };

    public static readonly string[] NflTeams =
    {
        "Chiefs", "49ers", "Eagles", "Ravens", "Bills", "Cowboys", "Packers", "Lions"
    };

    public static readonly string[] MlbTeams =
    {
        "Yankees", "Dodgers", "Astros", "Braves", "Phillies", "Rangers", "Orioles"
    };

    public static readonly string[] FifaNationalTeams =
    {
        "Brazil", "Argentina", "France", "England", "Spain", "Germany", "Portugal", "Italy"
    };
}
