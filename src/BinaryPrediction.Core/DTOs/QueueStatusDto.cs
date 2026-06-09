namespace BinaryPrediction.Core.DTOs;

public class QueueStatusDto
{
    public int PendingAnalyses { get; set; }
    public int ProcessingAnalyses { get; set; }
    public int CompletedAnalyses { get; set; }
    public int FailedAnalyses { get; set; }
    
    public bool HasAlert => FailedAnalyses > 10;
}
