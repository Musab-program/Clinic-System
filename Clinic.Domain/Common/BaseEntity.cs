namespace Clinic.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public string? LastModifiedBy { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
    public bool IsDeleted { get; set; } = false; // Soft Delete
}