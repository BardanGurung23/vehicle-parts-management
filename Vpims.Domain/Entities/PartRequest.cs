namespace Vpims.Domain.Entities;

public sealed class PartRequest
{
    public int RequestId { get; set; }

    public int CustomerId { get; set; }

    public int? VehicleId { get; set; }

    public string RequestedPartName { get; set; } = string.Empty;

    public string? RequestDetails { get; set; }

    public string Status { get; set; } = "Pending";

    public DateTimeOffset RequestedAt { get; set; }

    public DateTimeOffset? ResolvedAt { get; set; }

    public Customer? Customer { get; set; }

    public Vehicle? Vehicle { get; set; }
}