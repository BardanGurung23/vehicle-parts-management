namespace Vpims.Application.DTOs.Vendors;

public sealed class VendorResponse
{
    public int VendorId { get; set; }

    public string VendorName { get; set; } = string.Empty;

    public string? ContactPerson { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
