using System;
using System.ComponentModel.DataAnnotations;

namespace BinaryPrediction.Core.Entities;

/// <summary>
/// Daily snapshot of edge detection opportunities.
/// </summary>
public class OpportunityAnalyticsSnapshot
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>Date (UTC) of the snapshot.</summary>
    public DateOnly SnapshotDateUtc { get; set; }

    /// <summary>Total number of opportunities detected on this day.</summary>
    public int OpportunityCount { get; set; }

    /// <summary>Average gap percentage across all opportunities.</summary>
    public decimal AverageGapPercentage { get; set; }

    /// <summary>Largest gap percentage observed.</summary>
    public decimal LargestGapPercentage { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
