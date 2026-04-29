namespace Vpims.Application.DTOs.Appointments;

public sealed class CreateAppointmentRequest
{
    public int VehicleId { get; set; }
    public DateTimeOffset AppointmentDate { get; set; }
    public string ServiceType { get; set; } = string.Empty;
    public string? Notes { get; set; }
}