using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vpims.Application.DTOs.Users;
using Vpims.Application.Interfaces.Services;

namespace Vpims.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/staff")]
public sealed class AdminStaffController(IStaffManagementService staffManagementService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StaffUserResponse>>> GetStaff(CancellationToken cancellationToken)
    {
        IReadOnlyList<StaffUserResponse> response = await staffManagementService.GetStaffUsersAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType<StaffUserResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<StaffUserResponse>> CreateStaff(
        [FromBody] CreateStaffUserRequest request,
        CancellationToken cancellationToken)
    {
        StaffUserResponse response = await staffManagementService.RegisterStaffAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetStaff), new { id = response.UserId }, response);
    }

    [HttpPut("{userId:int}/role")]
    public async Task<ActionResult<StaffUserResponse>> UpdateRole(
        int userId,
        [FromBody] UpdateStaffRoleRequest request,
        CancellationToken cancellationToken)
    {
        StaffUserResponse response = await staffManagementService.UpdateStaffRoleAsync(userId, request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("roles")]
    public async Task<ActionResult<IReadOnlyList<RoleOptionResponse>>> GetAssignableRoles(CancellationToken cancellationToken)
    {
        IReadOnlyList<RoleOptionResponse> response = await staffManagementService.GetAssignableRolesAsync(cancellationToken);
        return Ok(response);
    }
}