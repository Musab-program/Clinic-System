namespace Clinic.Infrastructure.Persistence;

using Clinic.Application.Common.Interfaces;
using Clinic.Domain.Common;
using Clinic.Domain.Entities;
using Clinic.Domain.Entities.Authentication;
using Clinic.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Reflection;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }


    public DbSet<Doctor> Doctors { get; set; } = null!;
    public DbSet<Specialty> Specialties { get; set; } = null!;
    public DbSet<DoctorSpecialty> DoctorSpecialties { get; set; } = null!;
    public DbSet<DoctorWorkingHour> DoctorWorkingHours { get; set; } = null!;
    public DbSet<Patient> Patients { get; set; } = null!;
    public DbSet<Appointment> Appointments { get; set; } = null!;
    public DbSet<AppointmentService> AppointmentServices { get; set; } = null!;
    public DbSet<AppointmentStatusHistory> AppointmentStatusHistories { get; set; } = null!;
    public DbSet<ServiceItem> ServiceItems { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    public Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default) =>
        Database.BeginTransactionAsync(isolationLevel, cancellationToken);

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // تحديث التواريخ تلقائياً فقط
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAtUtc == default)
                {
                    entry.Entity.CreatedAtUtc = now;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // جداول Identity
        builder.Entity<ApplicationUser>(entity => entity.ToTable("Users"));
        builder.Entity<IdentityRole<Guid>>(entity => entity.ToTable("Roles"));
        builder.Entity<IdentityUserRole<Guid>>(entity => entity.ToTable("UserRoles"));
        builder.Entity<IdentityUserClaim<Guid>>(entity => entity.ToTable("UserClaims"));
        builder.Entity<IdentityUserLogin<Guid>>(entity => entity.ToTable("UserLogins"));
        builder.Entity<IdentityRoleClaim<Guid>>(entity => entity.ToTable("RoleClaims"));
        builder.Entity<IdentityUserToken<Guid>>(entity => entity.ToTable("UserTokens"));
        builder.Entity<RefreshToken>().Property(token => token.Token).HasMaxLength(512).IsRequired();
        builder.Entity<RefreshToken>().HasIndex(token => token.Token).IsUnique();

        // قراءة الـ Configurations من الـ Assembly
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}