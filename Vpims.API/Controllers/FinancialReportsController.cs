using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vpims.Application.DTOs.Reports;
using Vpims.Application.Interfaces.Services;

namespace Vpims.API.Controllers;

[ApiController]
[Route("api/admin/reports/financial")]
[Authorize(Roles = "Admin")]
public sealed class FinancialReportsController(IFinancialReportService financialReportService) : ControllerBase
{
    [HttpGet("daily")]
    [ProducesResponseType<FinancialReportResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<FinancialReportResponse>> GetDaily(
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        FinancialReportResponse report = await financialReportService.GetDailyReportAsync(date, cancellationToken);
        return Ok(report);
    }

    [HttpGet("monthly")]
    [ProducesResponseType<FinancialReportResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<FinancialReportResponse>> GetMonthly(
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken cancellationToken)
    {
        FinancialReportResponse report = await financialReportService.GetMonthlyReportAsync(year, month, cancellationToken);
        return Ok(report);
    }

    [HttpGet("yearly")]
    [ProducesResponseType<FinancialReportResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<FinancialReportResponse>> GetYearly(
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        FinancialReportResponse report = await financialReportService.GetYearlyReportAsync(year, cancellationToken);
        return Ok(report);
    }

    [HttpGet("all-time")]
    [ProducesResponseType<FinancialReportResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<FinancialReportResponse>> GetAllTime(CancellationToken cancellationToken)
    {
        FinancialReportResponse report = await financialReportService.GetAllTimeReportAsync(cancellationToken);
        return Ok(report);
    }
}