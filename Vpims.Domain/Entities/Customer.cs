namespace Vpims.Domain.Entities;

public sealed class Customer
{
    public int CustomerId { get; set; }

    public int UserId { get; set; }

    public string? Address { get; set; }

    public DateTimeOffset RegisteredAt { get; set; }

    public User? User { get; set; }
}