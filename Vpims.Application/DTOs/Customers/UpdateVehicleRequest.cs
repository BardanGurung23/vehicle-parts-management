using System.ComponentModel.DataAnnotations;

namespace Vpims.Application.DTOs.Customers;

public sealed class UpdateVehicleRequest
{
    [Required]
    [StringLength(30, MinimumLength = 2)]
    public string VehicleNumber { get; set; } = string.Empty;

    [StringLength(80)]
    public string? Model { get; set; }

    [Range(0, 2_000_000)]
    public int? Mileage { get; set; }

    [Range(1950, 2100)]
    public int? ManufactureYear { get; set; }

    public DateTimeOffset? LastServiceDate { get; set; }
}