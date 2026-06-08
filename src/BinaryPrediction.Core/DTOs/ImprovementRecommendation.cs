namespace BinaryPrediction.Core.DTOs;

public class ImprovementRecommendation
{
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int Severity { get; set; }
}
