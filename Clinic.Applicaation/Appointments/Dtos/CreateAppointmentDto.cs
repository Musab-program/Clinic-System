namespace Clinic.Application.DTOs.Appointments;

public class CreateAppointmentDto
{
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid SpecialtyId { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public int DurationMinutes { get; set; }

    public List<AppointmentServiceItemDto> SelectedServices { get; set; } = new();
}
