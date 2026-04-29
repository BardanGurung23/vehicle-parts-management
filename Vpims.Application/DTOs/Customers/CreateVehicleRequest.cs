namespace Vpims.Application.DTOs.Customers;

public sealed class CreateVehicleRequest
{
    public string VehicleNumber { get; set; } = string.Empty;
    public string? Model { get; set; }
}