using Microsoft.EntityFrameworkCore;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Domain.Entities;
using Vpims.Infrastructure.Persistence;

namespace Vpims.Infrastructure.Repositories;

public sealed class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken);
    }

    public Task<bool> ExistsByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        return dbContext.Users.AnyAsync(user => user.PhoneNumber == phoneNumber, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .Include(user => user.Role)
            .Include(user => user.Customer)
            .FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
    }

    public async Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .Include(user => user.Role)
            .Include(user => user.Customer)
            .FirstOrDefaultAsync(user => user.UserId == userId, cancellationToken);
    }

    public async Task<User> CreateStaffAsync(User user, CancellationToken cancellationToken = default)
    {
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetRequiredUserAsync(user.UserId, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetStaffUsersAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Include(user => user.Role)
            .Where(user => user.Role != null && SystemRoles.StaffAssignableRoles.Contains(user.Role.Name))
            .OrderBy(user => user.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetUsersByRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Include(user => user.Role)
            .Where(user => user.Role != null && user.Role.Name == roleName)
            .OrderBy(user => user.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task<User> UpdateRoleAsync(User user, int roleId, CancellationToken cancellationToken = default)
    {
        user.RoleId = roleId;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetRequiredUserAsync(user.UserId, cancellationToken);
    }

    public async Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetRequiredUserAsync(user.UserId, cancellationToken);
    }

    private async Task<User> GetRequiredUserAsync(int userId, CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Include(user => user.Role)
            .Include(user => user.Customer)
            .FirstAsync(user => user.UserId == userId, cancellationToken);
    }
}