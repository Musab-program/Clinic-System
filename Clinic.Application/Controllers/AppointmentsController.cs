using Clinic.Application.Common.Interfaces;
using Clinic.Application.DTOs.Appointments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentsController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    [Authorize(Policy = "ChangeAppointmentStatus")]
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeAppointmentStatusDto dto, CancellationToken ct)
    {
        await _appointmentService.ChangeStatusAsync(id, dto, ct);
        return NoContent();
    }

    [Authorize(Policy = "Staff")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAppointment(Guid id, [FromBody] UpdateAppointmentDto dto, CancellationToken ct)
    {
        await _appointmentService.UpdateAppointmentAsync(id, dto, ct);
        return NoContent();
    }

    [Authorize(Policy = "Staff")]
    [HttpPost("{id:guid}/payments")]
    public async Task<IActionResult> ProcessPayment(Guid id, [FromBody] ProcessPaymentDto dto, CancellationToken ct)
    {
        await _appointmentService.ProcessPaymentAsync(id, dto.Amount, dto.PaymentMethod, ct);
        return Ok(new { message = "Payment processed successfully." });
    }

    [Authorize(Policy = "BookAppointment")]
    [HttpPost]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentDto dto, CancellationToken ct)
    {
        var appointmentId = await _appointmentService.CreateAppointmentAsync(dto, ct);
        return StatusCode(StatusCodes.Status201Created, new { id = appointmentId, message = "Appointment created successfully." });
    }
}
