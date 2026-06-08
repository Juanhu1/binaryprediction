namespace BinaryPrediction.Core.Common;

public static class OutcomeNormalizer
{
    public static string Normalize(string? rawOutcome)
    {
        if (string.IsNullOrWhiteSpace(rawOutcome))
        {
            return "Unknown";
        }

        var normalized = rawOutcome.Trim().ToLowerInvariant();

        return normalized switch
        {
            "yes" or "true" or "1" => "Yes",
            "no" or "false" or "0" => "No",
            _ => "Unknown"
        };
    }
}
