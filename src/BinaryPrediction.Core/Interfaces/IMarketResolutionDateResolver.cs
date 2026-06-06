namespace BinaryPrediction.Core.Interfaces;

public interface IMarketResolutionDateResolver
{
    (DateTimeOffset? Date, string ResolutionMethod) ResolveDate(string? question, DateTimeOffset? explicitEndDate, DateTimeOffset? alternativeDate);
}
