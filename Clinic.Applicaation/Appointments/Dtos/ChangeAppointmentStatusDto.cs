using Clinic.Domain.Enums;

namespace Clinic.Application.DTOs.Appointments;

public class ChangeAppointmentStatusDto
{
    public AppointmentStatus NewStatus { get; set; }
    public string? Notes { get; set; }
}
