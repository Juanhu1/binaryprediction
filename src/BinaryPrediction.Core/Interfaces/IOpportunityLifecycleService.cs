using System;
using System.Threading;
using System.Threading.Tasks;
using BinaryPrediction.Core.DTOs;
using BinaryPrediction.Core.Enums;
namespace BinaryPrediction.Core.Interfaces
{
    /// <summary>
    /// Service responsible for managing the lifecycle of a PredictionOpportunity.
    /// </summary>
    public interface IOpportunityLifecycleService
    {
        /// <summary>
        /// Retrieves detailed information for a specific opportunity.
        /// </summary>
        Task<OpportunityDetailDto?> GetOpportunityAsync(Guid opportunityId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Changes the status of an opportunity and records the transition.
        /// </summary>
        Task ChangeStatusAsync(Guid opportunityId, OpportunityStatus newStatus, string reason, CancellationToken cancellationToken = default);
    }
}
