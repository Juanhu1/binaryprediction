namespace BinaryPrediction.Core.DTOs;

public class BenchmarkComparisonDto
{
    public BenchmarkResultDto Ai { get; set; } = new();
    public BenchmarkResultDto AlwaysYes { get; set; } = new();
    public BenchmarkResultDto AlwaysNo { get; set; } = new();
    public BenchmarkResultDto Random { get; set; } = new();
}
