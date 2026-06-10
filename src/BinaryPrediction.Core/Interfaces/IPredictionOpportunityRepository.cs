using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BinaryPrediction.Core.Entities;

namespace BinaryPrediction.Core.Interfaces;

/// <summary>
/// Repository for accessing PredictionOpportunity records.
/// </summary>
public interface IPredictionOpportunityRepository
{
    Task<PredictionOpportunity?> GetByPredictionIdAsync(Guid predictionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PredictionOpportunity>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PredictionOpportunity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(PredictionOpportunity opportunity, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
