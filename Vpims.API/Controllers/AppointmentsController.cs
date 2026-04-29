using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vpims.Application.DTOs.Appointments;
using Vpims.Application.DTOs.Auth;
using Vpims.Application.Interfaces.Services;

namespace Vpims.API.Controllers;

[ApiController]
[Route("api/appointments")]
public sealed class AppointmentsController(
    IAppointmentService appointmentService,
    IAuthService authService) : ControllerBase
{
    [Authorize(Roles = "Customer")]
    [HttpPost]
    [ProducesResponseType<AppointmentResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<AppointmentResponse>> Create(
        [FromBody] CreateAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        UserProfileResponse currentUser = await authService.GetCurrentUserAsync(User, cancellationToken);
        AppointmentResponse response = await appointmentService.CreateAppointmentAsync(currentUser, request, cancellationToken);
        return Created($"/api/appointments/{response.AppointmentId}", response);
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("me")]
    [ProducesResponseType<IReadOnlyList<AppointmentResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AppointmentResponse>>> GetMyAppointments(CancellationToken cancellationToken)
    {
        UserProfileResponse currentUser = await authService.GetCurrentUserAsync(User, cancellationToken);
        IReadOnlyList<AppointmentResponse> response = await appointmentService.GetCustomerAppointmentsAsync(currentUser, cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpGet("list")]
    [ProducesResponseType<IReadOnlyList<AppointmentResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AppointmentResponse>>> GetAll(CancellationToken cancellationToken)
    {
        IReadOnlyList<AppointmentResponse> response = await appointmentService.GetAllAppointmentsAsync(cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = "Admin,Staff,Customer")]
    [HttpGet("{appointmentId:int}")]
    [ProducesResponseType<AppointmentResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AppointmentResponse>> GetById(int appointmentId, CancellationToken cancellationToken)
    {
        AppointmentResponse response = await appointmentService.GetAppointmentByIdAsync(appointmentId, cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPatch("{appointmentId:int}/status")]
    [ProducesResponseType<AppointmentResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AppointmentResponse>> UpdateStatus(
        int appointmentId,
        [FromBody] UpdateAppointmentStatusRequest request,
        CancellationToken cancellationToken)
    {
        AppointmentResponse response = await appointmentService.UpdateStatusAsync(appointmentId, request, cancellationToken);
        return Ok(response);
    }
}