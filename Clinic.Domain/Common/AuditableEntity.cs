namespace Clinic.Domain.Common;

/// <summary>Base entity with additional audit timestamps.</summary>
public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
}
