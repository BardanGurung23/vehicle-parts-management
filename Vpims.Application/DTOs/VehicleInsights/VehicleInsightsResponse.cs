namespace Vpims.Application.DTOs.VehicleInsights;

public sealed class VehicleInsightsResponse
{
    public int VehicleId { get; set; }

    public string VehicleNumber { get; set; } = string.Empty;

    public string? Model { get; set; }

    public int HealthScore { get; set; }

    public string HealthStatus { get; set; } = string.Empty;

    public string UsagePattern { get; set; } = string.Empty;

    public int? Mileage { get; set; }

    public int? ManufactureYear { get; set; }

    public int? VehicleAgeYears { get; set; }

    public int? EstimatedAnnualMileage { get; set; }

    public DateTimeOffset? LastServiceDate { get; set; }

    public DateTimeOffset GeneratedAt { get; set; }

    public IReadOnlyList<VehicleInsightItemResponse> Insights { get; set; } = [];
}