using System;
using System.Threading;
using System.Threading.Tasks;
using BinaryPrediction.Core.Interfaces;

namespace BinaryPrediction.Core.Interfaces;

/// <summary>
/// Service that evaluates a prediction for an edge (significant probability gap) and records an opportunity.
/// </summary>
public interface IEdgeDetectionService
{
    /// <summary>
    /// Analyzes the specified prediction and creates/updates a <see cref="PredictionOpportunity"/> if an edge is found.
    /// </summary>
    Task DetectOpportunityAsync(Guid predictionId, CancellationToken cancellationToken = default);
}
