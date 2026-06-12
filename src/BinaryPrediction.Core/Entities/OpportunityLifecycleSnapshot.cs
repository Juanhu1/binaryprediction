using System;
using System.ComponentModel.DataAnnotations;

namespace BinaryPrediction.Core.Entities;

/// <summary>
/// Daily snapshot aggregating counts of opportunities per lifecycle status.
/// </summary>
public class OpportunityLifecycleSnapshot
{
    [Key]
    public Guid Id { get; set; }

    public DateTimeOffset SnapshotDateUtc { get; set; }

    public int OpenCount { get; set; }
    public int ActiveCount { get; set; }
    public int IgnoredCount { get; set; }
    public int ExpiredCount { get; set; }
    public int ResolvedCount { get; set; }
}
