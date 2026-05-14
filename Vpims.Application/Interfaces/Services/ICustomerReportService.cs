using Vpims.Application.DTOs.Reports;

namespace Vpims.Application.Interfaces.Services;

public interface ICustomerReportService
{
    Task<CustomerReportsResponse> GetReportAsync(
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        decimal? highSpenderThreshold = null,
        CancellationToken cancellationToken = default);
}