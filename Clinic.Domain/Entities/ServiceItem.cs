using Clinic.Domain.Common;

namespace Clinic.Domain.Entities;

public class ServiceItem : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }

    // Navigation properties
    public ICollection<AppointmentService> AppointmentServices { get; set; } = new List<AppointmentService>();
}
