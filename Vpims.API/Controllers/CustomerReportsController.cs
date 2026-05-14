using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vpims.Application.DTOs.Reports;
using Vpims.Application.Interfaces.Services;

namespace Vpims.API.Controllers;

[ApiController]
[Route("api/reports/customers")]
[Authorize(Roles = "Admin,Staff")]
public sealed class CustomerReportsController(ICustomerReportService customerReportService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<CustomerReportsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerReportsResponse>> Get(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] decimal? highSpenderThreshold,
        CancellationToken cancellationToken)
    {
        CustomerReportsResponse report = await customerReportService.GetReportAsync(
            startDate,
            endDate,
            highSpenderThreshold,
            cancellationToken);

        return Ok(report);
    }
}