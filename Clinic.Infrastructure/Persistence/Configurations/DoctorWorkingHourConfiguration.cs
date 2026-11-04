using Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class DoctorWorkingHourConfiguration : IEntityTypeConfiguration<DoctorWorkingHour>
{
    public void Configure(EntityTypeBuilder<DoctorWorkingHour> builder)
    {
        builder.ToTable("DoctorWorkingHours");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.DayOfWeek)
            .IsRequired();

        builder.Property(w => w.StartTime)
            .IsRequired();

        builder.Property(w => w.EndTime)
            .IsRequired();



        builder.HasIndex(w => new { w.DoctorId, w.DayOfWeek });

        builder.HasQueryFilter(w => !w.IsDeleted);
    }
}