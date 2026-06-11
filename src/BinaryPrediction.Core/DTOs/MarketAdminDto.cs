using System;

namespace BinaryPrediction.Core.DTOs;

/// <summary>
/// Market information for the admin dashboard.
/// </summary>
public class MarketAdminDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal MarketProbability { get; set; }
    public string Status { get; set; } = string.Empty; // Active / Closed / Inactive
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? CloseTimeUtc { get; set; }
}
