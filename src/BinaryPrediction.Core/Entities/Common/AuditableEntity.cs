namespace BinaryPrediction.Core.Entities.Common;

public abstract class AuditableEntity : BaseEntity
{
    public DateTimeOffset? UpdatedAtUtc { get; set; }
}
