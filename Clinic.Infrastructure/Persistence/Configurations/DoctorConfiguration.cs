using Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("Doctors");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.ApplicationUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(d => d.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.Bio)
            .HasMaxLength(1000);

        builder.Property(d => d.ConsultationFee)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(d => d.MaxDailyAppointments)
            .IsRequired()
            .HasDefaultValue(20);

        builder.HasIndex(d => d.ApplicationUserId)
            .IsUnique();

        builder.HasMany(d => d.WorkingHours)
            .WithOne(w => w.Doctor)
            .HasForeignKey(w => w.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(d => !d.IsDeleted);
    }
}