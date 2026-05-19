namespace Vpims.Application.DTOs.VehicleInsights;

public sealed class VehicleInsightItemResponse
{
    public string Code { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string RiskLevel { get; set; } = string.Empty;

    public string RecommendedAction { get; set; } = string.Empty;

    public string PredictedTimeframe { get; set; } = string.Empty;
}