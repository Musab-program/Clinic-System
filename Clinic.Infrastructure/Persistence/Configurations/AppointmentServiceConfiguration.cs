using Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clinic.Infrastructure.Persistence.Configurations
{
    public class AppointmentServiceConfiguration : IEntityTypeConfiguration<AppointmentService>
    {
        public void Configure(EntityTypeBuilder<AppointmentService> builder)
        {
            builder.ToTable("AppointmentServices");

            builder.HasKey(aserv => aserv.Id);

            builder.Property(aserv => aserv.UnitPrice)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(aserv => aserv.Quantity)
                .IsRequired()
                .HasDefaultValue(1);

            builder.HasOne(aserv => aserv.Appointment)
                .WithMany(a => a.AppointmentServices)
                .HasForeignKey(aserv => aserv.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(aserv => aserv.ServiceItem)
                .WithMany(s => s.AppointmentServices)
                .HasForeignKey(aserv => aserv.ServiceItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(aserv => !aserv.IsDeleted);
        }
    }

}
