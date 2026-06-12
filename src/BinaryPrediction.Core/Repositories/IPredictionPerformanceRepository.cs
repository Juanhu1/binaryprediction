using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BinaryPrediction.Core.Entities;

namespace BinaryPrediction.Core.Repositories;

public interface IPredictionPerformanceRepository
{
    Task AddAsync(PredictionPerformanceSnapshot snapshot, CancellationToken cancellationToken = default);
    Task<PredictionPerformanceSnapshot?> GetCurrentAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PredictionPerformanceSnapshot>> GetTrendAsync(DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken = default);

    // Evaluation queries
    Task<IReadOnlyList<Prediction>> GetUnevaluatedResolvedPredictionsAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetAccuracyAsync(DateTime? dateUtc = null, CancellationToken cancellationToken = default);
    Task<decimal> GetAverageConfidenceAsync(DateTime? dateUtc = null, CancellationToken cancellationToken = default);
    Task<decimal> GetAverageBrierScoreAsync(DateTime? dateUtc = null, CancellationToken cancellationToken = default);
}
