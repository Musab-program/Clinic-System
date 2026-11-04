using Clinic.Domain.Common;

namespace Clinic.Domain.Entities;

public class Specialty : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? BaseConsultationFee { get; set; }

    public Guid? ParentSpecialtyId { get; set; }
    public Specialty? ParentSpecialty { get; set; }
    public ICollection<Specialty> SubSpecialties { get; set; } = new List<Specialty>();

    public ICollection<DoctorSpecialty> DoctorSpecialties { get; set; } = new List<DoctorSpecialty>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}