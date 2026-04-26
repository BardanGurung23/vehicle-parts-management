namespace Vpims.Application.DTOs.Customers;

public sealed class RegisterCustomerResponse
{
    public int UserId { get; set; }

    public int CustomerId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string? Address { get; set; }

    public IReadOnlyList<VehicleResponse> Vehicles { get; set; } = [];
}