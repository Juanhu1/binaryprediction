using System;
using System.ComponentModel.DataAnnotations;
using BinaryPrediction.Core.Enums;

namespace BinaryPrediction.Core.Entities;

/// <summary>
/// Audit record tracking status changes of a PredictionOpportunity.
/// </summary>
public class OpportunityStatusHistory
{
    [Key]
    public Guid Id { get; set; }

    public Guid OpportunityId { get; set; }
    public PredictionOpportunity? Opportunity { get; set; }

    public OpportunityStatus PreviousStatus { get; set; }
    public OpportunityStatus NewStatus { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset ChangedAtUtc { get; set; }
}
