using Vpims.Domain.Entities;

namespace Vpims.Application.Interfaces.Repositories;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(int roleId, CancellationToken cancellationToken = default);

    Task<Role?> GetByNameAsync(string roleName, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Role>> GetAssignableStaffRolesAsync(CancellationToken cancellationToken = default);
}