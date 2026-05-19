using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vpims.Application.DTOs.Dev;
using Vpims.Application.Interfaces.Services;

namespace Vpims.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/dev")]
public sealed class DevController(
    IDevEmailService devEmailService,
    IWebHostEnvironment environment) : ControllerBase
{
    [HttpPost("test-email")]
    [ProducesResponseType<TestEmailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TestEmailResponse>> SendTestEmail(
        [FromBody] TestEmailRequest request,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        TestEmailResponse response = await devEmailService.SendTestEmailAsync(request, cancellationToken);
        return Ok(response);
    }
}