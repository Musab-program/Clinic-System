using Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.ApplicationUserId)
            .HasMaxLength(450);

        builder.Property(p => p.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.LastName)
            .IsRequired()
            .HasMaxLength(100);



        builder.Property(p => p.BloodGroup)
            .HasMaxLength(10);

        builder.Property(p => p.Address)
            .HasMaxLength(250);

        builder.Property(p => p.Gender)
            .IsRequired();

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}