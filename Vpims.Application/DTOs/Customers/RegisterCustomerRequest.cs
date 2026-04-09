using System.ComponentModel.DataAnnotations;

namespace Vpims.Application.DTOs.Customers;

public sealed class RegisterCustomerRequest
{
    [Required]
    [StringLength(150, MinimumLength = 3)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(20, MinimumLength = 7)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Address { get; set; }
}