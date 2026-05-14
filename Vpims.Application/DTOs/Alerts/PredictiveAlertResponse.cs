namespace Vpims.Application.DTOs.Alerts;

public sealed class PredictiveAlertResponse
{
    public int PredictiveAlertId { get; set; }

    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public int VehicleId { get; set; }

    public string VehicleNumber { get; set; } = string.Empty;

    public int? PartId { get; set; }

    public string? PartName { get; set; }

    public string AlertMessage { get; set; } = string.Empty;

    public string RiskLevel { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}