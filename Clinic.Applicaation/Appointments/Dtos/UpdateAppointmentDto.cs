namespace Clinic.Application.DTOs.Appointments;

public class UpdateAppointmentDto
{
    public DateTime StartTimeUtc { get; set; }
    public int DurationMinutes { get; set; }
}