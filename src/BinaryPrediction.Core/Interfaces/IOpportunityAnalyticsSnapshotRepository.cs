using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BinaryPrediction.Core.Entities;

namespace BinaryPrediction.Core.Interfaces;

/// <summary>
/// Repository for accessing OpportunityAnalyticsSnapshot records.
/// </summary>
public interface IOpportunityAnalyticsSnapshotRepository
{
    Task AddAsync(OpportunityAnalyticsSnapshot snapshot, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OpportunityAnalyticsSnapshot>> GetAllAsync(CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
