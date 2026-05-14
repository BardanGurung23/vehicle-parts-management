using Vpims.Domain.Entities;

namespace Vpims.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> ExistsByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default);

    Task<User> CreateStaffAsync(User user, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetStaffUsersAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetUsersByRoleAsync(string roleName, CancellationToken cancellationToken = default);

    Task<User> UpdateRoleAsync(User user, int roleId, CancellationToken cancellationToken = default);
}