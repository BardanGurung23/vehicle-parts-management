using System.ComponentModel.DataAnnotations;

namespace Vpims.Application.DTOs.Customers;

public sealed class CreateCustomerRequest
{
    [Required]
    [StringLength(150, MinimumLength = 3)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(20, MinimumLength = 7)]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [Required]
    [StringLength(30, MinimumLength = 2)]
    public string VehicleNumber { get; set; } = string.Empty;

    [StringLength(80)]
    public string? VehicleModel { get; set; }
}