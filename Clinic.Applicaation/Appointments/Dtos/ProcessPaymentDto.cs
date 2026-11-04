using Clinic.Domain.Enums;

namespace Clinic.Application.DTOs.Appointments;

public class ProcessPaymentDto
{
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
}