using Clinic.Domain.Entities;
using Clinic.Domain.Enums;
using Clinic.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Clinic.Infrastructure.Persistence;

/// <summary>Creates local demo data only when explicitly enabled in Development.</summary>
public static class DatabaseSeeder
{
    private const string AdminEmail = "admin@clinic.local";
    private const string ReceptionEmail = "reception@clinic.local";
    private const string DoctorEmail = "doctor@clinic.local";
    private const string PatientEmail = "patient@clinic.local";

    // Local development accounts only. Change these before sharing any development database.
    private const string DemoPassword = "ClinicDemo123!";

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var environment = services.GetRequiredService<IHostEnvironment>();
        if (!environment.IsDevelopment())
            throw new InvalidOperationException("Demo data seeding is restricted to the Development environment.");

        var db = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        await db.Database.MigrateAsync(cancellationToken);

        foreach (var roleName in new[] { "Admin", "Receptionist", "Doctor", "Patient" })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                EnsureSucceeded(result, $"create role {roleName}");
            }
        }

        await EnsureUserAsync(userManager, AdminEmail, "ClinicAdmin", "Admin");
        await EnsureUserAsync(userManager, ReceptionEmail, "ClinicReception", "Receptionist");
        var doctorUser = await EnsureUserAsync(userManager, DoctorEmail, "DemoDoctor", "Doctor");
        var patientUser = await EnsureUserAsync(userManager, PatientEmail, "DemoPatient", "Patient");

        var parentSpecialty = await db.Specialties.FirstOrDefaultAsync(s => s.Name == "General Medicine", cancellationToken);
        if (parentSpecialty is null)
        {
            parentSpecialty = new Specialty
            {
                Id = Guid.NewGuid(),
                Name = "General Medicine",
                Description = "Demo parent specialty",
                BaseConsultationFee = 100m
            };
            db.Specialties.Add(parentSpecialty);
            await db.SaveChangesAsync(cancellationToken);
        }

        var childSpecialty = await db.Specialties.FirstOrDefaultAsync(s => s.Name == "Pediatrics Demo", cancellationToken);
        if (childSpecialty is null)
        {
            childSpecialty = new Specialty
            {
                Id = Guid.NewGuid(),
                Name = "Pediatrics Demo",
                Description = "Demo child specialty; inherits the parent consultation fee",
                ParentSpecialtyId = parentSpecialty.Id,
                BaseConsultationFee = null
            };
            db.Specialties.Add(childSpecialty);
            await db.SaveChangesAsync(cancellationToken);
        }

        var doctor = await db.Doctors.FirstOrDefaultAsync(d => d.ApplicationUserId == doctorUser.Id.ToString(), cancellationToken);
        if (doctor is null)
        {
            doctor = new Doctor
            {
                Id = Guid.NewGuid(),
                ApplicationUserId = doctorUser.Id.ToString(),
                FirstName = "Demo",
                LastName = "Doctor",
                Bio = "Development-only sample account",
                ConsultationFee = 100m,
                MaxDailyAppointments = 20
            };
            db.Doctors.Add(doctor);
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.DoctorSpecialties.AnyAsync(ds => ds.DoctorId == doctor.Id && ds.SpecialtyId == parentSpecialty.Id, cancellationToken))
        {
            db.DoctorSpecialties.Add(new DoctorSpecialty
            {
                Id = Guid.NewGuid(),
                DoctorId = doctor.Id,
                SpecialtyId = parentSpecialty.Id,
                IsPrimary = true
            });
        }

        var patient = await db.Patients.FirstOrDefaultAsync(p => p.ApplicationUserId == patientUser.Id.ToString(), cancellationToken);
        if (patient is null)
        {
            patient = new Patient
            {
                Id = Guid.NewGuid(),
                ApplicationUserId = patientUser.Id.ToString(),
                FirstName = "Demo",
                LastName = "Patient",
                DateOfBirth = new DateOnly(1995, 5, 10),
                Gender = Gender.Female,
                Address = "Development test data"
            };
            db.Patients.Add(patient);
        }

        var service = await db.ServiceItems.FirstOrDefaultAsync(s => s.Name == "Demo Lab Test", cancellationToken);
        if (service is null)
        {
            service = new ServiceItem
            {
                Id = Guid.NewGuid(),
                Name = "Demo Lab Test",
                Description = "Development-only sample service",
                Price = 25m
            };
            db.ServiceItems.Add(service);
        }

        await db.SaveChangesAsync(cancellationToken);

        var workDays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
        foreach (var day in workDays)
        {
            if (!await db.DoctorWorkingHours.AnyAsync(h => h.DoctorId == doctor.Id && h.DayOfWeek == day, cancellationToken))
            {
                db.DoctorWorkingHours.Add(new DoctorWorkingHour
                {
                    Id = Guid.NewGuid(),
                    DoctorId = doctor.Id,
                    DayOfWeek = day,
                    StartTime = new TimeOnly(8, 0),
                    EndTime = new TimeOnly(17, 0)
                });
            }
        }
        await db.SaveChangesAsync(cancellationToken);

        Console.WriteLine("ClinicSystem demo seed complete (Development only).");
        Console.WriteLine($"Admin: {AdminEmail} / {DemoPassword}");
        Console.WriteLine($"Receptionist: {ReceptionEmail} / {DemoPassword}");
        Console.WriteLine($"Doctor: {DoctorEmail} / {DemoPassword} (DoctorId: {doctor.Id})");
        Console.WriteLine($"Patient: {PatientEmail} / {DemoPassword} (PatientId: {patient.Id})");
        Console.WriteLine($"Parent specialty ID: {parentSpecialty.Id}");
        Console.WriteLine($"Child specialty ID (inherits parent fee): {childSpecialty.Id}");
        Console.WriteLine($"Demo service ID: {service.Id}");
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string userName,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = userName,
                Email = email,
                EmailConfirmed = true,
                Status = AccountStatus.Active
            };
            var createResult = await userManager.CreateAsync(user, DemoPassword);
            EnsureSucceeded(createResult, $"create demo account {email}");
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var roleResult = await userManager.AddToRoleAsync(user, role);
            EnsureSucceeded(roleResult, $"assign role {role} to {email}");
        }

        return user;
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Could not {operation}: {errors}");
        }
    }
}
