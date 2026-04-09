using Microsoft.EntityFrameworkCore;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Domain.Entities;
using Vpims.Infrastructure.Persistence;

namespace Vpims.Infrastructure.Repositories;

public sealed class RoleRepository(AppDbContext dbContext) : IRoleRepository
{
    public async Task<Role?> GetByIdAsync(int roleId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(role => role.RoleId == roleId, cancellationToken);
    }

    public async Task<Role?> GetByNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        return await dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(role => role.Name == roleName, cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetAssignableStaffRolesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Roles
            .AsNoTracking()
            .Where(role => SystemRoles.StaffAssignableRoles.Contains(role.Name))
            .OrderBy(role => role.Name)
            .ToListAsync(cancellationToken);
    }
}