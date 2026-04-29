namespace Vpims.Application.DTOs.Reviews;

public sealed class ReviewResponse
{
    public int ReviewId { get; set; }
    public int AppointmentId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}