namespace Vpims.Application.DTOs.Alerts;

public sealed class AlertSummaryResponse
{
    public int ActiveAlertCount { get; set; }

    public int LowStockAlertCount { get; set; }

    public int OverdueCreditAlertCount { get; set; }

    public int PredictiveAlertCount { get; set; }

    public DateTimeOffset GeneratedAt { get; set; }

    public IReadOnlyList<LowStockAlertResponse> LowStockAlerts { get; set; } = [];

    public IReadOnlyList<OverdueCreditAlertResponse> OverdueCreditAlerts { get; set; } = [];

    public IReadOnlyList<PredictiveAlertResponse> PredictiveAlerts { get; set; } = [];
}