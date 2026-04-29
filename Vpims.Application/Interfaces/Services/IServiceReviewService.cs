using Vpims.Application.DTOs.Appointments;
using Vpims.Application.DTOs.Auth;
using Vpims.Application.DTOs.Reviews;

namespace Vpims.Application.Interfaces.Services;

public interface IServiceReviewService
{
    Task<ReviewResponse> CreateReviewAsync(
        UserProfileResponse currentUser,
        CreateReviewRequest request,
        CancellationToken cancellationToken = default);

    Task<ReviewResponse?> GetReviewByAppointmentIdAsync(
        int appointmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReviewResponse>> GetCustomerReviewsAsync(
        UserProfileResponse currentUser,
        CancellationToken cancellationToken = default);
}