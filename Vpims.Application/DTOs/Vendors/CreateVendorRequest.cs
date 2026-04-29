using System.ComponentModel.DataAnnotations;

namespace Vpims.Application.DTOs.Vendors;

public sealed class CreateVendorRequest
{
    [Required]
    [MaxLength(150)]
    public string VendorName { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? ContactPerson { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [MaxLength(150)]
    [EmailAddress]
    public string? Email { get; set; }

    public string? Address { get; set; }
}
