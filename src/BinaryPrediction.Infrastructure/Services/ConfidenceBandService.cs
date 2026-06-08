using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Interfaces;

namespace BinaryPrediction.Infrastructure.Services;

public class ConfidenceBandService : IConfidenceBandService
{
    private readonly IPredictionStatisticsService _statisticsService;

    public ConfidenceBandService(IPredictionStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    public async Task<List<ConfidenceBandPerformanceDto>> GetConfidenceBandPerformanceAsync(CancellationToken cancellationToken = default)
    {
        var results = await _statisticsService.GetConfidenceBandResultsAsync(cancellationToken);

        return results.Select(r => new ConfidenceBandPerformanceDto
        {
            BandName = r.BandName,
            PredictionCount = r.TotalPredictions,
            CorrectCount = r.CorrectPredictions,
            AccuracyPercentage = r.AccuracyPercentage,
            AverageBrierScore = r.AverageBrierScore
        }).ToList();
    }
}
