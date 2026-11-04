using Clinic.Domain.Common;

namespace Clinic.Domain.Entities;

public class DoctorSpecialty : BaseEntity
{
    public Guid DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;

    public Guid SpecialtyId { get; set; }
    public Specialty Specialty { get; set; } = null!;

    public bool IsPrimary { get; set; } = false;
}