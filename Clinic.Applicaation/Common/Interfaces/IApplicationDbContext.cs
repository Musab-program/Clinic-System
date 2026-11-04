using System.Data;
using Clinic.Domain.Entities;
using Clinic.Domain.Entities.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Collections.Generic;

namespace Clinic.Application.Common.Interfaces;

public interface IApplicationDbContext
{

    DbSet<Doctor> Doctors { get; }
    DbSet<Specialty> Specialties { get; }
    DbSet<DoctorSpecialty> DoctorSpecialties { get; }
    DbSet<DoctorWorkingHour> DoctorWorkingHours { get; }
    DbSet<RefreshToken> RefreshTokens { get; set; } 

    DbSet<Patient> Patients { get; }
    DbSet<Appointment> Appointments { get; }
    DbSet<AppointmentService> AppointmentServices { get; }
    DbSet<AppointmentStatusHistory> AppointmentStatusHistories { get; }
    DbSet<ServiceItem> ServiceItems { get; }
    DbSet<Payment> Payments { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
