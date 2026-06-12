using System;
using BinaryPrediction.Core.Enums;

namespace BinaryPrediction.Api.DTOs;

/// <summary>
/// Data transfer object representing a single status change entry for an opportunity.
/// </summary>
public class OpportunityStatusHistoryDto
{
    public Guid Id { get; set; }
    public OpportunityStatus PreviousStatus { get; set; }
    public OpportunityStatus NewStatus { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset ChangedAtUtc { get; set; }
}
