using BinaryPrediction.Core.Entities.Common;

namespace BinaryPrediction.Core.Entities;

public class PromptVersion : BaseEntity
{
    public string Version { get; set; } = string.Empty;
    public string PromptName { get; set; } = string.Empty;
    public string PromptTemplate { get; set; } = string.Empty;
}
