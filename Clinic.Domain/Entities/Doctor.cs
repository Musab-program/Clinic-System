using Clinic.Domain.Common;

namespace Clinic.Domain.Entities;


public class Doctor : BaseEntity
{
    public string ApplicationUserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public decimal ConsultationFee { get; set; }
    public int MaxDailyAppointments { get; set; } = 20;

    public ICollection<DoctorSpecialty> DoctorSpecialties { get; set; } = new List<DoctorSpecialty>();
    public ICollection<DoctorWorkingHour> WorkingHours { get; set; } = new List<DoctorWorkingHour>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}