using System.ComponentModel.DataAnnotations;

namespace Vpims.Domain.Entities;

public sealed class Vendor
{
    public int VendorId { get; set; }

    [Required]
    [MaxLength(150)]
    public string VendorName { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? ContactPerson { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [MaxLength(150)]
    public string? Email { get; set; }

    public string? Address { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Part> Parts { get; set; } = new List<Part>();
}
