namespace Vpims.Domain.Entities;

public sealed class Appointment
{
    public int AppointmentId { get; set; }

    public int CustomerId { get; set; }

    public int VehicleId { get; set; }

    public DateTimeOffset AppointmentDate { get; set; }

    public string ServiceType { get; set; } = string.Empty;

    public string Status { get; set; } = "Pending";

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Customer? Customer { get; set; }

    public Vehicle? Vehicle { get; set; }

    public ServiceReview? Review { get; set; }
}