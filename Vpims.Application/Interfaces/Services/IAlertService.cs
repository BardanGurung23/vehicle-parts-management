using Vpims.Application.DTOs.Alerts;

namespace Vpims.Application.Interfaces.Services;

public interface IAlertService
{
    Task<AlertSummaryResponse> GetAlertSummaryAsync(CancellationToken cancellationToken = default);

    Task<AlertSummaryResponse> GenerateAlertsAsync(CancellationToken cancellationToken = default);
}