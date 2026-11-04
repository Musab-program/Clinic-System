using Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class DoctorSpecialtyConfiguration : IEntityTypeConfiguration<DoctorSpecialty>
{
    public void Configure(EntityTypeBuilder<DoctorSpecialty> builder)
    {
        builder.ToTable("DoctorSpecialties");

        builder.HasKey(ds => ds.Id);

        builder.Property(ds => ds.IsPrimary)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(ds => new { ds.DoctorId, ds.SpecialtyId })
            .IsUnique();

        builder.HasOne(ds => ds.Doctor)
            .WithMany(d => d.DoctorSpecialties)
            .HasForeignKey(ds => ds.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ds => ds.Specialty)
            .WithMany(s => s.DoctorSpecialties)
            .HasForeignKey(ds => ds.SpecialtyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(ds => !ds.IsDeleted);
    }
}