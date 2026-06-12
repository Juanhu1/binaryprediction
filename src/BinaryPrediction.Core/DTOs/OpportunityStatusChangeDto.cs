using System;
using BinaryPrediction.Core.Enums;

namespace BinaryPrediction.Core.DTOs
{
    /// <summary>
    /// Request payload for changing an opportunity's status.
    /// </summary>
    public class OpportunityStatusChangeDto
    {
        public OpportunityStatus NewStatus { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
