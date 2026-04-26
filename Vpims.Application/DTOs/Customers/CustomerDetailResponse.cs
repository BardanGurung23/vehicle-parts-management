namespace Vpims.Application.DTOs.Customers;

public sealed class CustomerDetailResponse
{
    public int CustomerId { get; set; }

    public int? UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Address { get; set; }

    public DateTimeOffset RegisteredAt { get; set; }

    public IReadOnlyList<VehicleResponse> Vehicles { get; set; } = [];
}