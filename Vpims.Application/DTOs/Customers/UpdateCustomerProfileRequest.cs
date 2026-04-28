using System.ComponentModel.DataAnnotations;

namespace Vpims.Application.DTOs.Customers;

public sealed class UpdateCustomerProfileRequest
{
    [Required]
    [StringLength(150, MinimumLength = 3)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }
}
