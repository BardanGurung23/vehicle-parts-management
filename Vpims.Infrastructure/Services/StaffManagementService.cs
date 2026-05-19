using Microsoft.AspNetCore.Identity;
using Vpims.Application.Common.Exceptions;
using Vpims.Application.DTOs.Users;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Application.Interfaces.Services;
using Vpims.Domain.Entities;

namespace Vpims.Infrastructure.Services;

public sealed class StaffManagementService(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    PasswordHasher<User> passwordHasher) : IStaffManagementService
{
    public async Task<StaffUserResponse> RegisterStaffAsync(CreateStaffUserRequest request, CancellationToken cancellationToken = default)
    {
        Role role = await GetAssignableRoleAsync(request.RoleId, cancellationToken);

        string email = InputNormalizer.NormalizeEmail(request.Email);
        string phoneNumber = InputNormalizer.NormalizePhoneNumber(request.PhoneNumber);
        string fullName = InputNormalizer.NormalizeFullName(request.FullName);

        await EnsureUniqueUserIdentityAsync(email, phoneNumber, cancellationToken);

        var user = new User
        {
            RoleId = role.RoleId,
            FullName = fullName,
            Email = email,
            PhoneNumber = phoneNumber,
            CreatedAt = DateTimeOffset.UtcNow,
            IsActive = true
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        User createdUser = await userRepository.CreateStaffAsync(user, cancellationToken);

        return UserMapper.ToStaffResponse(createdUser);
    }

    public async Task<IReadOnlyList<StaffUserResponse>> GetStaffUsersAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<User> users = await userRepository.GetStaffUsersAsync(cancellationToken);
        return users.Select(UserMapper.ToStaffResponse).ToList();
    }

    public async Task<StaffUserResponse> UpdateStaffRoleAsync(int userId, UpdateStaffRoleRequest request, CancellationToken cancellationToken = default)
    {
        Role role = await GetAssignableRoleAsync(request.RoleId, cancellationToken);

        User user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Staff user not found.");

        if (user.Role?.Name == SystemRoles.Customer)
        {
            throw new AppValidationException("Customer accounts cannot be managed from the staff area.");
        }

        User updatedUser = await userRepository.UpdateRoleAsync(user, role.RoleId, cancellationToken);

        return UserMapper.ToStaffResponse(updatedUser);
    }

    public async Task<StaffUserResponse> DeactivateStaffUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        User user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Staff user not found.");

        if (user.Role?.Name == SystemRoles.Customer)
        {
            throw new AppValidationException("Customer accounts cannot be managed from the staff area.");
        }

        if (!user.IsActive)
        {
            throw new AppValidationException("Staff user is already inactive.");
        }

        user.IsActive = false;

        User updatedUser = await userRepository.UpdateAsync(user, cancellationToken);
        return UserMapper.ToStaffResponse(updatedUser);
    }

    public async Task<IReadOnlyList<RoleOptionResponse>> GetAssignableRolesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Role> roles = await roleRepository.GetAssignableStaffRolesAsync(cancellationToken);
        return roles.Select(UserMapper.ToRoleResponse).ToList();
    }

    private async Task<Role> GetAssignableRoleAsync(int roleId, CancellationToken cancellationToken)
    {
        Role role = await roleRepository.GetByIdAsync(roleId, cancellationToken)
            ?? throw new NotFoundException("Role not found.");

        if (!SystemRoles.StaffAssignableRoles.Contains(role.Name))
        {
            throw new AppValidationException("Only Admin and Staff roles can be assigned here.");
        }

        return role;
    }

    private async Task EnsureUniqueUserIdentityAsync(string email, string phoneNumber, CancellationToken cancellationToken)
    {
        if (await userRepository.ExistsByEmailAsync(email, cancellationToken))
        {
            throw new AppValidationException("A user with this email already exists.");
        }

        if (await userRepository.ExistsByPhoneNumberAsync(phoneNumber, cancellationToken))
        {
            throw new AppValidationException("A user with this phone number already exists.");
        }
    }
}