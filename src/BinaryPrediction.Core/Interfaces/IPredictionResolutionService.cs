using BinaryPrediction.Core.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BinaryPrediction.Core.Interfaces;

public interface IPredictionResolutionService
{
    /// <summary>
    /// Processes all pending predictions for markets that have been resolved.
    /// Returns the number of predictions processed.
    /// </summary>
    Task<int> ProcessPendingPredictionsAsync(CancellationToken cancellationToken = default);
}
