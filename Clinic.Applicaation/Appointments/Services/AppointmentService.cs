using System.Data;
using Clinic.Application.Common.Exceptions;
using Clinic.Application.Common.Interfaces;
using Clinic.Application.DTOs.Appointments;
using Clinic.Domain.Entities;
using Clinic.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Application.Services;

public sealed class AppointmentService : IAppointmentService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public AppointmentService(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task ChangeStatusAsync(Guid appointmentId, ChangeAppointmentStatusDto dto, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Appointment not found.");
        if (appointment.Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
            throw new InvalidOperationException("A completed or cancelled appointment is in a final state.");
        if (appointment.Status == dto.NewStatus)
            throw new InvalidOperationException("Appointment already has this status.");

        await AuthorizeStatusChangeAsync(appointment, dto.NewStatus, cancellationToken);
        var valid = (appointment.Status, dto.NewStatus) switch
        {
            (AppointmentStatus.Scheduled, AppointmentStatus.Confirmed) => true,
            (AppointmentStatus.Scheduled, AppointmentStatus.Cancelled) => true,
            (AppointmentStatus.Confirmed, AppointmentStatus.Completed) => true,
            (AppointmentStatus.Confirmed, AppointmentStatus.Cancelled) => true,
            _ => false
        };
        if (!valid)
            throw new InvalidOperationException($"Invalid status transition: {appointment.Status} -> {dto.NewStatus}.");

        var previousStatus = appointment.Status;
        appointment.Status = dto.NewStatus;
        _context.AppointmentStatusHistories.Add(new AppointmentStatusHistory
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointment.Id,
            OldStatus = previousStatus,
            NewStatus = dto.NewStatus,
            ChangedByUserId = _currentUser.UserId ?? "system",
            ChangedAtUtc = DateTime.UtcNow,
            Note = dto.Notes
        });
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateAppointmentAsync(Guid appointmentId, UpdateAppointmentDto dto, CancellationToken cancellationToken = default)
    {
        ValidateDuration(dto.DurationMinutes);
        await using var transaction = await _context.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Appointment not found.");
        if (appointment.Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
            throw new InvalidOperationException("A completed or cancelled appointment cannot be changed.");

        var end = dto.StartTimeUtc.AddMinutes(dto.DurationMinutes);
        await EnsureScheduleAllowsAsync(appointment.DoctorId, appointment.PatientId, dto.StartTimeUtc, end, appointment.Id, cancellationToken);
        appointment.StartTimeUtc = dto.StartTimeUtc;
        appointment.DurationMinutes = dto.DurationMinutes;
        appointment.EndTimeUtc = end;
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<Guid> CreateAppointmentAsync(CreateAppointmentDto dto, CancellationToken cancellationToken = default)
    {
        ValidateDuration(dto.DurationMinutes);
        await using var transaction = await _context.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        if (!await _context.Patients.AnyAsync(p => p.Id == dto.PatientId, cancellationToken))
            throw new KeyNotFoundException("Patient not found.");
        if (!await _context.Doctors.AnyAsync(d => d.Id == dto.DoctorId, cancellationToken))
            throw new KeyNotFoundException("Doctor not found.");
        await AuthorizeBookingAsync(dto.PatientId, cancellationToken);

        var specialty = await _context.Specialties.Include(s => s.ParentSpecialty)
            .FirstOrDefaultAsync(s => s.Id == dto.SpecialtyId, cancellationToken)
            ?? throw new KeyNotFoundException("Specialty not found.");
        var qualified = await _context.DoctorSpecialties.AnyAsync(ds => ds.DoctorId == dto.DoctorId &&
            (ds.SpecialtyId == dto.SpecialtyId || ds.SpecialtyId == specialty.ParentSpecialtyId), cancellationToken);
        if (!qualified)
            throw new InvalidOperationException("The selected doctor does not practice this specialty or its parent specialty.");

        var end = dto.StartTimeUtc.AddMinutes(dto.DurationMinutes);
        await EnsureScheduleAllowsAsync(dto.DoctorId, dto.PatientId, dto.StartTimeUtc, end, null, cancellationToken);

        var selectedServices = dto.SelectedServices ?? new List<AppointmentServiceItemDto>();
        if (selectedServices.Any(s => s.Quantity <= 0))
            throw new InvalidOperationException("Service quantities must be positive.");
        var serviceIds = selectedServices.Select(s => s.ServiceItemId).Distinct().ToList();
        var servicePrices = serviceIds.Count == 0
            ? new Dictionary<Guid, decimal>()
            : await _context.ServiceItems.Where(s => serviceIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Price, cancellationToken);
        if (servicePrices.Count != serviceIds.Count)
            throw new KeyNotFoundException("One or more selected service items do not exist.");

        var consultationFee = specialty.BaseConsultationFee ?? specialty.ParentSpecialty?.BaseConsultationFee
            ?? throw new InvalidOperationException("Neither this specialty nor its parent has a consultation fee.");
        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = dto.PatientId,
            DoctorId = dto.DoctorId,
            SpecialtyId = dto.SpecialtyId,
            StartTimeUtc = dto.StartTimeUtc,
            DurationMinutes = dto.DurationMinutes,
            EndTimeUtc = end,
            Status = AppointmentStatus.Scheduled,
            TotalFee = consultationFee + selectedServices.Sum(s => servicePrices[s.ServiceItemId] * s.Quantity)
        };
        foreach (var item in selectedServices)
        {
            _context.AppointmentServices.Add(new Clinic.Domain.Entities.AppointmentService
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointment.Id,
                ServiceItemId = item.ServiceItemId,
                Quantity = item.Quantity,
                UnitPrice = servicePrices[item.ServiceItemId]
            });
        }
        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return appointment.Id;
    }

    public async Task ProcessPaymentAsync(Guid appointmentId, decimal paymentAmount, PaymentMethod paymentMethod, CancellationToken cancellationToken = default)
    {
        if (paymentAmount <= 0)
            throw new InvalidOperationException("Payment amount must be greater than zero.");
        await using var transaction = await _context.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Appointment not found.");
        var paid = await _context.Payments.Where(p => p.AppointmentId == appointmentId && p.Status == PaymentStatus.Paid)
            .SumAsync(p => p.Amount, cancellationToken);
        if (paid + paymentAmount > appointment.TotalFee)
            throw new InvalidOperationException($"Payment exceeds the remaining balance of {appointment.TotalFee - paid}.");

        _context.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointmentId,
            Amount = paymentAmount,
            Method = paymentMethod,
            Status = PaymentStatus.Paid,
            PaidAtUtc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task AuthorizeBookingAsync(Guid patientId, CancellationToken cancellationToken)
    {
        if (_currentUser.IsInRole("Admin") || _currentUser.IsInRole("Receptionist"))
            return;
        if (!_currentUser.IsInRole("Patient") || string.IsNullOrWhiteSpace(_currentUser.UserId))
            throw new ForbiddenAccessException("Only a patient or authorized staff member can create this appointment.");
        var ownsPatientRecord = await _context.Patients.AnyAsync(p => p.Id == patientId && p.ApplicationUserId == _currentUser.UserId, cancellationToken);
        if (!ownsPatientRecord)
            throw new ForbiddenAccessException("Patients can only book appointments for their own profile.");
    }

    private async Task AuthorizeStatusChangeAsync(Appointment appointment, AppointmentStatus target, CancellationToken cancellationToken)
    {
        if (_currentUser.IsInRole("Admin") || _currentUser.IsInRole("Receptionist"))
            return;
        if (string.IsNullOrWhiteSpace(_currentUser.UserId))
            throw new ForbiddenAccessException("A user identity is required.");

        if (_currentUser.IsInRole("Patient") && target == AppointmentStatus.Cancelled)
        {
            var ownsAppointment = await _context.Patients.AnyAsync(p => p.Id == appointment.PatientId && p.ApplicationUserId == _currentUser.UserId, cancellationToken);
            if (!ownsAppointment)
                throw new ForbiddenAccessException("Patients can only cancel their own appointments.");
            if (appointment.StartTimeUtc - DateTime.UtcNow < TimeSpan.FromHours(2))
                throw new InvalidOperationException("Patients may cancel only at least two hours before the appointment.");
            return;
        }

        if (_currentUser.IsInRole("Doctor") && target is AppointmentStatus.Confirmed or AppointmentStatus.Completed)
        {
            var ownsAppointment = await _context.Doctors.AnyAsync(d => d.Id == appointment.DoctorId && d.ApplicationUserId == _currentUser.UserId, cancellationToken);
            if (!ownsAppointment)
                throw new ForbiddenAccessException("Doctors can only update appointments assigned to them.");
            return;
        }
        throw new ForbiddenAccessException("You are not allowed to perform this appointment status change.");
    }

    private async Task EnsureScheduleAllowsAsync(Guid doctorId, Guid patientId, DateTime start, DateTime end, Guid? exceptAppointmentId, CancellationToken cancellationToken)
    {
        if (start.Kind != DateTimeKind.Utc)
            throw new InvalidOperationException("Appointment start time must be UTC.");
        if (start <= DateTime.UtcNow)
            throw new InvalidOperationException("Appointment must be scheduled in the future.");
        if (start.Date != end.Date)
            throw new InvalidOperationException("Appointments cannot cross midnight.");

        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId, cancellationToken)
            ?? throw new KeyNotFoundException("Doctor not found.");
        var hours = await _context.DoctorWorkingHours.FirstOrDefaultAsync(h => h.DoctorId == doctorId && h.DayOfWeek == start.DayOfWeek, cancellationToken)
            ?? throw new InvalidOperationException("The doctor is not working on this day.");
        var startTime = TimeOnly.FromDateTime(start);
        var endTime = TimeOnly.FromDateTime(end);
        if (startTime < hours.StartTime || endTime > hours.EndTime)
            throw new InvalidOperationException("Appointment falls outside the doctor's working hours.");

        var activeStatuses = new[] { AppointmentStatus.Scheduled, AppointmentStatus.Confirmed };
        var doctorOverlap = await _context.Appointments.AnyAsync(a => a.DoctorId == doctorId &&
            (!exceptAppointmentId.HasValue || a.Id != exceptAppointmentId.Value) && activeStatuses.Contains(a.Status) &&
            start < a.EndTimeUtc && end > a.StartTimeUtc, cancellationToken);
        if (doctorOverlap)
            throw new InvalidOperationException("The doctor already has an appointment during this time.");

        var patientOverlap = await _context.Appointments.AnyAsync(a => a.PatientId == patientId &&
            (!exceptAppointmentId.HasValue || a.Id != exceptAppointmentId.Value) && activeStatuses.Contains(a.Status) &&
            start < a.EndTimeUtc && end > a.StartTimeUtc, cancellationToken);
        if (patientOverlap)
            throw new InvalidOperationException("The patient already has an appointment during this time.");

        var dayStart = start.Date;
        var dayEnd = dayStart.AddDays(1);
        var dailyCount = await _context.Appointments.CountAsync(a => a.DoctorId == doctorId &&
            (!exceptAppointmentId.HasValue || a.Id != exceptAppointmentId.Value) && activeStatuses.Contains(a.Status) &&
            a.StartTimeUtc >= dayStart && a.StartTimeUtc < dayEnd, cancellationToken);
        if (dailyCount >= doctor.MaxDailyAppointments)
            throw new InvalidOperationException("The doctor has reached the maximum number of appointments for this day.");
    }

    private static void ValidateDuration(int durationMinutes)
    {
        if (durationMinutes <= 0 || durationMinutes > 720)
            throw new InvalidOperationException("Duration must be between 1 and 720 minutes.");
    }
}
