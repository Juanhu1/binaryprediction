using BinaryPrediction.Core.Entities;
using BinaryPrediction.Core.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BinaryPrediction.Core.Interfaces;

public interface IPredictionResolutionRepository
{
    /// <summary>
    /// Returns all predictions that are pending evaluation for markets that have been resolved.
    /// A pending prediction has EvaluatedAtUtc == null and its Market.ResolvedAtUtc != null.
    /// </summary>
    Task<List<Prediction>> GetPendingPredictionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists any pending changes to the context.
    /// </summary>
    Task SaveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total number of predictions that have been resolved (EvaluatedAtUtc != null).
    /// </summary>
    Task<int> GetResolvedCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of pending predictions.
    /// </summary>
    Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the number of predictions that were resolved today (UTC).
    /// This counts predictions whose Market.ResolvedAtUtc falls on the current UTC date.
    /// </summary>
    Task<int> GetResolvedTodayAsync(CancellationToken cancellationToken = default);
}
