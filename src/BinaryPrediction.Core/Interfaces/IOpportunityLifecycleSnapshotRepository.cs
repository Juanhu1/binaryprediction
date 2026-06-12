using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BinaryPrediction.Core.Entities;

namespace BinaryPrediction.Core.Interfaces;

/// <summary>
/// Repository for accessing OpportunityLifecycleSnapshot records.
/// </summary>
public interface IOpportunityLifecycleSnapshotRepository
{
    Task AddAsync(OpportunityLifecycleSnapshot snapshot, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OpportunityLifecycleSnapshot>> GetAllAsync(CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
