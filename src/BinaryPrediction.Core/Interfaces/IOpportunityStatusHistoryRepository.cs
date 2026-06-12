using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BinaryPrediction.Core.Entities;

namespace BinaryPrediction.Core.Interfaces
{
    /// <summary>
    /// Repository for accessing OpportunityStatusHistory records.
    /// </summary>
    public interface IOpportunityStatusHistoryRepository
    {
        Task AddAsync(OpportunityStatusHistory history, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<OpportunityStatusHistory>> GetByOpportunityIdAsync(Guid opportunityId, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
