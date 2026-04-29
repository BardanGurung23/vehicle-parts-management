namespace Vpims.Application.DTOs.Vendors;

public sealed class UpdateVendorRequest
{
    public string? VendorName { get; set; }

    public string? ContactPerson { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }
}
