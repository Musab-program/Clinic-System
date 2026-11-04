using Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clinic.Infrastructure.Persistence.Configurations
{
    public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
    {
        public void Configure(EntityTypeBuilder<Appointment> builder)
        {
            builder.ToTable("Appointments");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.StartTimeUtc)
                .IsRequired();

            builder.Property(a => a.EndTimeUtc)
                .IsRequired();

            builder.Property(a => a.DurationMinutes)
                .IsRequired();

            builder.Property(a => a.Status)
                .IsRequired();

            builder.Property(a => a.TotalFee)
                .HasPrecision(18, 2)
                .IsRequired();



            builder.HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.Specialty)
                .WithMany(s => s.Appointments)
                .HasForeignKey(a => a.SpecialtyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(a => new { a.DoctorId, a.StartTimeUtc });
            builder.HasIndex(a => new { a.PatientId, a.StartTimeUtc });

            builder.HasQueryFilter(a => !a.IsDeleted);
        }
    }

}
