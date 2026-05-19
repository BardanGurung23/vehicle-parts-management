using Vpims.Application.DTOs.Users;

namespace Vpims.Application.Interfaces.Services;

public interface IStaffManagementService
{
    Task<StaffUserResponse> RegisterStaffAsync(CreateStaffUserRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StaffUserResponse>> GetStaffUsersAsync(CancellationToken cancellationToken = default);

    Task<StaffUserResponse> UpdateStaffRoleAsync(int userId, UpdateStaffRoleRequest request, CancellationToken cancellationToken = default);

    Task<StaffUserResponse> UpdateStaffAsync(int userId, UpdateStaffUserRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleOptionResponse>> GetAssignableRolesAsync(CancellationToken cancellationToken = default);
}