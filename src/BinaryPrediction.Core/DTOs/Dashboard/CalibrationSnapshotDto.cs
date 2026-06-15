namespace BinaryPrediction.Core.DTOs.Dashboard
{
    public class CalibrationSnapshotDto
    {
        public string ConfidenceRange { get; set; } = string.Empty;
        public int PredictionCount { get; set; }
        public decimal ExpectedAccuracy { get; set; }
        public decimal ActualAccuracy { get; set; }
        public decimal CalibrationError { get; set; }
    }
}
