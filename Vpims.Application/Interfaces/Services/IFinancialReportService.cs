using Vpims.Application.DTOs.Reports;

namespace Vpims.Application.Interfaces.Services;

public interface IFinancialReportService
{
    Task<FinancialReportResponse> GetDailyReportAsync(
        DateOnly? date = null,
        CancellationToken cancellationToken = default);

    Task<FinancialReportResponse> GetMonthlyReportAsync(
        int? year = null,
        int? month = null,
        CancellationToken cancellationToken = default);

    Task<FinancialReportResponse> GetYearlyReportAsync(
        int? year = null,
        CancellationToken cancellationToken = default);
}