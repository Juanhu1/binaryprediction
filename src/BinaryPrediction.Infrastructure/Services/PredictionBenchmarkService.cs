using BinaryPrediction.Core.Common;
using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BinaryPrediction.Infrastructure.Services;

public class PredictionBenchmarkService : IPredictionBenchmarkService
{
    private readonly IPredictionPerformanceRepository _performanceRepository;
    private readonly BinaryPredictionDbContext _dbContext;
    private readonly ILogger<PredictionBenchmarkService> _logger;

    public PredictionBenchmarkService(
        IPredictionPerformanceRepository performanceRepository,
        BinaryPredictionDbContext dbContext,
        ILogger<PredictionBenchmarkService> logger)
    {
        _performanceRepository = performanceRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<BenchmarkResultDto> EvaluateAlwaysYesAsync(CancellationToken cancellationToken = default)
    {
        return await EvaluateFixedBaselineAsync("Always Yes", "Yes", cancellationToken);
    }

    public async Task<BenchmarkResultDto> EvaluateAlwaysNoAsync(CancellationToken cancellationToken = default)
    {
        return await EvaluateFixedBaselineAsync("Always No", "No", cancellationToken);
    }

    public async Task<BenchmarkResultDto> EvaluateRandomAsync(CancellationToken cancellationToken = default)
    {
        var predictions = await _performanceRepository.GetEvaluatedPredictionsAsync(cancellationToken);
        if (!predictions.Any())
        {
            return new BenchmarkResultDto { BenchmarkType = "Random" };
        }

        var random = new Random(42); // deterministic seed for reproducibility
        int total = predictions.Count;
        int correct = 0;
        decimal brierSum = 0;

        foreach (var p in predictions)
        {
            string actual = OutcomeNormalizer.Normalize(p.ActualOutcome!);
            if (actual == "Unknown")
            {
                total--;
                continue;
            }

            string randomPrediction = random.Next(2) == 0 ? "Yes" : "No";
            
            if (randomPrediction == actual) correct++;

            // For random, P is effectively 0.5 or we treat confidence as 1.0 since it's a hard prediction?
            // "Random" benchmark implies P=0.5 if we want calibrated random, but if it generates "Yes" or "No", 
            // the confidence of that generated decision is 1.0 (or 0.5? Let's use 1.0 to match Always Yes/No, or 0.5?)
            // Actually, if it's a hard prediction Yes/No without confidence, let's treat confidence as 100%.
            decimal probOfYes = randomPrediction == "Yes" ? 1.0m : 0.0m;
            decimal actualYes = actual == "Yes" ? 1.0m : 0.0m;

            brierSum += (probOfYes - actualYes) * (probOfYes - actualYes);
        }

        var result = new BenchmarkResultDto
        {
            BenchmarkType = "Random",
            TotalPredictions = total,
            CorrectPredictions = correct,
            AccuracyPercentage = total > 0 ? (decimal)correct / total * 100m : 0,
            AverageBrierScore = total > 0 ? brierSum / total : 0
        };

        return result;
    }

    public async Task<BenchmarkResultDto> EvaluateAiAsync(CancellationToken cancellationToken = default)
    {
        var predictions = await _performanceRepository.GetEvaluatedPredictionsAsync(cancellationToken);
        var total = predictions.Count;
        var correct = predictions.Count(p => p.WasCorrect == true);
        var brierSum = predictions.Sum(p => p.BrierScore ?? 0);

        return new BenchmarkResultDto
        {
            BenchmarkType = "AI",
            TotalPredictions = total,
            CorrectPredictions = correct,
            AccuracyPercentage = total > 0 ? (decimal)correct / total * 100m : 0,
            AverageBrierScore = total > 0 ? brierSum / total : 0
        };
    }

    public async Task<BenchmarkComparisonDto> CompareAllAsync(CancellationToken cancellationToken = default)
    {
        var ai = await EvaluateAiAsync(cancellationToken);
        var alwaysYes = await EvaluateAlwaysYesAsync(cancellationToken);
        var alwaysNo = await EvaluateAlwaysNoAsync(cancellationToken);
        var random = await EvaluateRandomAsync(cancellationToken);

        _logger.LogInformation("Benchmark comparison completed.\nAI Accuracy: {AiAcc}%\nAlways Yes Accuracy: {YesAcc}%\nAlways No Accuracy: {NoAcc}%\nRandom Accuracy: {RandAcc}%",
            ai.AccuracyPercentage, alwaysYes.AccuracyPercentage, alwaysNo.AccuracyPercentage, random.AccuracyPercentage);

        return new BenchmarkComparisonDto
        {
            Ai = ai,
            AlwaysYes = alwaysYes,
            AlwaysNo = alwaysNo,
            Random = random
        };
    }

    private async Task<BenchmarkResultDto> EvaluateFixedBaselineAsync(string benchmarkType, string predictionValue, CancellationToken cancellationToken)
    {
        var predictions = await _performanceRepository.GetEvaluatedPredictionsAsync(cancellationToken);
        
        int total = predictions.Count;
        int correct = 0;
        decimal brierSum = 0;

        foreach (var p in predictions)
        {
            string actual = OutcomeNormalizer.Normalize(p.ActualOutcome!);
            if (actual == "Unknown")
            {
                total--;
                continue;
            }

            if (actual == predictionValue) correct++;

            decimal probOfYes = predictionValue == "Yes" ? 1.0m : 0.0m;
            decimal actualYes = actual == "Yes" ? 1.0m : 0.0m;
            brierSum += (probOfYes - actualYes) * (probOfYes - actualYes);
        }

        return new BenchmarkResultDto
        {
            BenchmarkType = benchmarkType,
            TotalPredictions = total,
            CorrectPredictions = correct,
            AccuracyPercentage = total > 0 ? (decimal)correct / total * 100m : 0,
            AverageBrierScore = total > 0 ? brierSum / total : 0
        };
    }
}
