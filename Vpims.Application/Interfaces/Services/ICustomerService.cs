using Vpims.Application.DTOs.Auth;
using Vpims.Application.DTOs.Customers;

namespace Vpims.Application.Interfaces.Services;

public interface ICustomerService
{
    Task<RegisterCustomerResponse> RegisterAsync(RegisterCustomerRequest request, CancellationToken cancellationToken = default);

    Task<CustomerDetailResponse> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerSearchResultResponse>> SearchCustomersAsync(SearchCustomersRequest request, CancellationToken cancellationToken = default);

    Task<CustomerDetailResponse> GetCustomerByIdAsync(int customerId, CancellationToken cancellationToken = default);

    Task<CustomerDetailResponse> GetCustomerByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    Task<CustomerDetailResponse> UpdateCustomerProfileAsync(
        UserProfileResponse currentUser,
        UpdateCustomerProfileRequest request,
        CancellationToken cancellationToken = default);

    Task<VehicleResponse> AddVehicleAsync(UserProfileResponse currentUser, CreateVehicleRequest request, CancellationToken cancellationToken = default);

    Task<CustomerDetailResponse> RemoveVehicleAsync(UserProfileResponse currentUser, int vehicleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VehicleResponse>> GetMyVehiclesAsync(UserProfileResponse currentUser, CancellationToken cancellationToken = default);
}