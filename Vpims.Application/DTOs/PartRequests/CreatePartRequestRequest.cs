namespace Vpims.Application.DTOs.PartRequests;

public sealed class CreatePartRequestRequest
{
    public int? VehicleId { get; set; }
    public string RequestedPartName { get; set; } = string.Empty;
    public string? RequestDetails { get; set; }
}