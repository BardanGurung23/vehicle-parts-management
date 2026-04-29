namespace Vpims.Domain.Entities;

public sealed class ServiceReview
{
    public int ReviewId { get; set; }

    public int AppointmentId { get; set; }

    public int CustomerId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Appointment? Appointment { get; set; }

    public Customer? Customer { get; set; }
}