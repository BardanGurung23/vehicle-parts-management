namespace Vpims.Application.DTOs.PartRequests;

public sealed class PartRequestResponse
{
    public int RequestId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int? VehicleId { get; set; }
    public string? VehicleNumber { get; set; }
    public string RequestedPartName { get; set; } = string.Empty;
    public string? RequestDetails { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
}