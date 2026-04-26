namespace Vpims.Domain.Entities;

public sealed class Vehicle
{
    public int VehicleId { get; set; }

    public int CustomerId { get; set; }

    public string VehicleNumber { get; set; } = string.Empty;

    public string? Model { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Customer? Customer { get; set; }
}