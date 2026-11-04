using System.Text.Json;
using Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Clinic.Infrastructure.Persistence.Audit;

public class AuditEntry
{
    public AuditEntry(EntityEntry entry)
    {
        Entry = entry;
    }

    public EntityEntry Entry { get; }
    public string? UserId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
    public Dictionary<string, object?> OldValues { get; } = new();
    public Dictionary<string, object?> NewValues { get; } = new();
    public List<string> ChangedColumns { get; } = new();

    public AuditLog ToAuditLog()
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            Action = Action,
            EntityName = EntityName,
            EntityId = EntityId,
            TimestampUtc = TimestampUtc,
            OldValues = OldValues.Count == 0 ? null : JsonSerializer.Serialize(OldValues),
            NewValues = NewValues.Count == 0 ? null : JsonSerializer.Serialize(NewValues)
        };
    }
}