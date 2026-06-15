using System;
using System.Threading;
using System.Threading.Tasks;

namespace BinaryPrediction.Infrastructure.Interfaces
{
    public interface IDataRepairService
    {
        /// <summary>
        /// Repairs WasCorrect values and recomputes PredictionError for all predictions where they are inconsistent.
        /// </summary>
        Task RepairAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes all PredictionOpportunity records and recomputes them based on current predictions.
        /// </summary>
        Task RecomputeAllOpportunitiesAsync(CancellationToken cancellationToken = default);
    }
}
