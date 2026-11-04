using Clinic.Application.DTOs.Appointments;
using Clinic.Domain.Enums;

namespace Clinic.Application.Common.Interfaces;

public interface IAppointmentService
{
    Task ChangeStatusAsync(Guid appointmentId, ChangeAppointmentStatusDto dto, CancellationToken cancellationToken = default);
    Task UpdateAppointmentAsync(Guid appointmentId, UpdateAppointmentDto dto, CancellationToken cancellationToken = default);
    Task<Guid> CreateAppointmentAsync(CreateAppointmentDto dto, CancellationToken cancellationToken = default);
    Task ProcessPaymentAsync(Guid appointmentId, decimal paymentAmount, PaymentMethod paymentMethod, CancellationToken cancellationToken = default);
}