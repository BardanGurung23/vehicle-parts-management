namespace Vpims.Application.DTOs.Reviews;

public sealed class CreateReviewRequest
{
    public int AppointmentId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}