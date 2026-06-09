using System;

namespace BinaryPrediction.Core.Entities;

public class PredictionPerformanceSnapshot
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public decimal DailyAccuracy { get; set; }
    public decimal DailyBrierScore { get; set; }
    public decimal WeeklyAccuracy { get; set; }
    public decimal WeeklyBrierScore { get; set; }
    public decimal MonthlyAccuracy { get; set; }
    public decimal MonthlyBrierScore { get; set; }
}
