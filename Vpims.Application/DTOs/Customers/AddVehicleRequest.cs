using System.ComponentModel.DataAnnotations;

namespace Vpims.Application.DTOs.Customers;

public sealed class AddVehicleRequest
{
    [Required]
    [StringLength(30, MinimumLength = 2)]
    public string VehicleNumber { get; set; } = string.Empty;

    [StringLength(80)]
    public string? VehicleModel { get; set; }
}