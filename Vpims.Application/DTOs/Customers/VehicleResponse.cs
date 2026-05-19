namespace Vpims.Application.DTOs.Customers;

public sealed class VehicleResponse
{
    public int VehicleId { get; set; }

    public string VehicleNumber { get; set; } = string.Empty;

    public string? Model { get; set; }

    public int? Mileage { get; set; }

    public int? ManufactureYear { get; set; }

    public DateTimeOffset? LastServiceDate { get; set; }
}