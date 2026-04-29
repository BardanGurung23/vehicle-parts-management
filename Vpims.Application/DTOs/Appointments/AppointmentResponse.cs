namespace Vpims.Application.DTOs.Appointments;

public sealed class AppointmentResponse
{
    public int AppointmentId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int VehicleId { get; set; }
    public string VehicleNumber { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    public DateTimeOffset AppointmentDate { get; set; }
    public string ServiceType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool HasReview { get; set; }
}