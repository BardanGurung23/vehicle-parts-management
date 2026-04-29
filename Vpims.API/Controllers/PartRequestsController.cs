using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vpims.Application.DTOs.Appointments;
using Vpims.Application.DTOs.Auth;
using Vpims.Application.DTOs.PartRequests;
using Vpims.Application.Interfaces.Services;

namespace Vpims.API.Controllers;

[ApiController]
[Route("api/part-requests")]
public sealed class PartRequestsController(
    IPartRequestService partRequestService,
    IAuthService authService) : ControllerBase
{
    [Authorize(Roles = "Customer")]
    [HttpPost]
    [ProducesResponseType<PartRequestResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PartRequestResponse>> Create(
        [FromBody] CreatePartRequestRequest request,
        CancellationToken cancellationToken)
    {
        UserProfileResponse currentUser = await authService.GetCurrentUserAsync(User, cancellationToken);
        PartRequestResponse response = await partRequestService.CreatePartRequestAsync(currentUser, request, cancellationToken);
        return Created($"/api/part-requests/{response.RequestId}", response);
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("me")]
    [ProducesResponseType<IReadOnlyList<PartRequestResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PartRequestResponse>>> GetMyPartRequests(CancellationToken cancellationToken)
    {
        UserProfileResponse currentUser = await authService.GetCurrentUserAsync(User, cancellationToken);
        IReadOnlyList<PartRequestResponse> response = await partRequestService.GetCustomerPartRequestsAsync(currentUser, cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PartRequestResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PartRequestResponse>>> GetAll(CancellationToken cancellationToken)
    {
        IReadOnlyList<PartRequestResponse> response = await partRequestService.GetAllPartRequestsAsync(cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = "Admin,Staff,Customer")]
    [HttpGet("{requestId:int}")]
    [ProducesResponseType<PartRequestResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PartRequestResponse>> GetById(int requestId, CancellationToken cancellationToken)
    {
        PartRequestResponse response = await partRequestService.GetPartRequestByIdAsync(requestId, cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPut("{requestId:int}/status")]
    [ProducesResponseType<PartRequestResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PartRequestResponse>> UpdateStatus(
        int requestId,
        [FromBody] UpdatePartRequestStatusRequest request,
        CancellationToken cancellationToken)
    {
        PartRequestResponse response = await partRequestService.UpdateStatusAsync(requestId, request.Status, cancellationToken);
        return Ok(response);
    }
}