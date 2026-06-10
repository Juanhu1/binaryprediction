using System;

namespace BinaryPrediction.Core.Options;

/// <summary>
/// Configuration options for the edge detection engine.
/// </summary>
public class EdgeDetectionOptions
{
    /// <summary>
    /// The gap percentage threshold at which an opportunity is considered an edge.
    /// Default is 10%.
    /// </summary>
    public decimal GapThresholdPercentage { get; set; } = 10;
}
