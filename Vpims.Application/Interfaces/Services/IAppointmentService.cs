using Vpims.Application.DTOs.Appointments;
using Vpims.Application.DTOs.Auth;

namespace Vpims.Application.Interfaces.Services;

public interface IAppointmentService
{
    Task<AppointmentResponse> CreateAppointmentAsync(
        UserProfileResponse currentUser,
        CreateAppointmentRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AppointmentResponse>> GetCustomerAppointmentsAsync(
        UserProfileResponse currentUser,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AppointmentResponse>> GetCustomerAppointmentsByCustomerIdAsync(
        int customerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AppointmentResponse>> GetAllAppointmentsAsync(
        CancellationToken cancellationToken = default);

    Task<AppointmentResponse> GetAppointmentByIdAsync(
        int appointmentId,
        CancellationToken cancellationToken = default);

    Task<AppointmentResponse> UpdateStatusAsync(
        int appointmentId,
        UpdateAppointmentStatusRequest request,
        CancellationToken cancellationToken = default);
}