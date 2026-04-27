using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vpims.Application.DTOs.Auth;
using Vpims.Application.DTOs.Dashboard;
using Vpims.Application.Interfaces.Services;

namespace Vpims.API.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController(
    IDashboardService dashboardService,
    IAuthService authService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryResponse>> GetSummary(CancellationToken cancellationToken)
    {
        UserProfileResponse currentUser = await authService.GetCurrentUserAsync(User, cancellationToken);
        DashboardSummaryResponse response = await dashboardService.GetSummaryAsync(currentUser, cancellationToken);
        return Ok(response);
    }
}