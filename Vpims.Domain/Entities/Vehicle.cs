namespace Vpims.Domain.Entities;

public sealed class Vehicle
{
    public int VehicleId { get; set; }

    public int CustomerId { get; set; }

    public string VehicleNumber { get; set; } = string.Empty;

    public string? Model { get; set; }

    public int? Mileage { get; set; }

    public int? ManufactureYear { get; set; }

    public DateTimeOffset? LastServiceDate { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Customer? Customer { get; set; }
}