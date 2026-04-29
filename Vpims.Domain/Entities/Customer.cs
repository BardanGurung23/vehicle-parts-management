namespace Vpims.Domain.Entities;

public sealed class Customer
{
    public int CustomerId { get; set; }

    public int? UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Address { get; set; }

    public DateTimeOffset RegisteredAt { get; set; }

    public User? User { get; set; }

    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();

    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}