namespace BinaryPrediction.Core.DTOs;

public class AiAnalysisResultDto
{
    public decimal EstimatedProbability { get; set; }
    public decimal Confidence { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<string> KeyReasons { get; set; } = new();
    public List<string> RiskFactors { get; set; } = new();
}
