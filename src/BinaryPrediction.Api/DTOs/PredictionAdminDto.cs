using System;

namespace BinaryPrediction.Api.DTOs;

/// <summary>
/// Prediction information for the admin dashboard.
/// </summary>
public class PredictionAdminDto
{
    public Guid PredictionId { get; set; }
    public string MarketTitle { get; set; } = string.Empty;
    public decimal AiProbability { get; set; }
    public decimal Confidence { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public bool Resolved { get; set; }
    public bool? OutcomeCorrect { get; set; }
}
