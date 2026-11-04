using Clinic.Domain.Common;

namespace Clinic.Domain.Entities;

public class AppointmentService : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = null!;

    public Guid ServiceItemId { get; set; }
    public ServiceItem ServiceItem { get; set; } = null!;

    
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
}
