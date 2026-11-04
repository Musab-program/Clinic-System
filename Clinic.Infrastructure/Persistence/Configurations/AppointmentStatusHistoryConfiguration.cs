using Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AppointmentStatusHistoryConfiguration : IEntityTypeConfiguration<AppointmentStatusHistory>
{
    public void Configure(EntityTypeBuilder<AppointmentStatusHistory> builder)
    {
        builder.ToTable("AppointmentStatusHistories");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.NewStatus)
            .IsRequired();

        builder.Property(h => h.ChangedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(h => h.ChangedAtUtc)
            .IsRequired();

        builder.Property(h => h.Note)
            .HasMaxLength(500);

        builder.HasOne(h => h.Appointment)
            .WithMany(a => a.StatusHistories)
            .HasForeignKey(h => h.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(h => !h.IsDeleted);
    }
}