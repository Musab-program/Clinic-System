namespace Clinic.Application.DTOs.Appointments;

public class AppointmentServiceItemDto
{
    public Guid ServiceItemId { get; set; }
    public int Quantity { get; set; } = 1;
}
