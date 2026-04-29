using Vpims.Application.DTOs.Appointments;
using Vpims.Application.DTOs.Auth;
using Vpims.Application.DTOs.PartRequests;

namespace Vpims.Application.Interfaces.Services;

public interface IPartRequestService
{
    Task<PartRequestResponse> CreatePartRequestAsync(
        UserProfileResponse currentUser,
        CreatePartRequestRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PartRequestResponse>> GetCustomerPartRequestsAsync(
        UserProfileResponse currentUser,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PartRequestResponse>> GetAllPartRequestsAsync(
        CancellationToken cancellationToken = default);

    Task<PartRequestResponse> GetPartRequestByIdAsync(
        int requestId,
        CancellationToken cancellationToken = default);

    Task<PartRequestResponse> UpdateStatusAsync(
        int requestId,
        string status,
        CancellationToken cancellationToken = default);
}