using Clinic.Domain.Common;
using Clinic.Domain.Enums;

namespace Clinic.Domain.Entities;



public class Appointment : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;

    public Guid SpecialtyId { get; set; }
    public Specialty Specialty { get; set; } = null!;

    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public int DurationMinutes { get; set; }
    public decimal TotalFee { get; set; } 
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

    public ICollection<AppointmentService> AppointmentServices { get; set; } = new List<AppointmentService>();
    public ICollection<AppointmentStatusHistory> StatusHistories { get; set; } = new List<AppointmentStatusHistory>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
