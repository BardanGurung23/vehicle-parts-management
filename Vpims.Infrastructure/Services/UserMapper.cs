using Vpims.Application.DTOs.Auth;
using Vpims.Application.DTOs.Customers;
using Vpims.Application.DTOs.Users;
using Vpims.Domain.Entities;

namespace Vpims.Infrastructure.Services;

internal static class UserMapper
{
    public static UserProfileResponse ToProfile(User user)
    {
        return new UserProfileResponse
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role?.Name ?? string.Empty,
            IsActive = user.IsActive,
            CustomerId = user.Customer?.CustomerId
        };
    }

    public static RegisterCustomerResponse ToRegistrationResponse(User user, Customer customer)
    {
        return new RegisterCustomerResponse
        {
            UserId = user.UserId,
            CustomerId = customer.CustomerId,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber
        };
    }

    public static StaffUserResponse ToStaffResponse(User user)
    {
        return new StaffUserResponse
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role?.Name ?? string.Empty,
            RoleId = user.RoleId,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }

    public static RoleOptionResponse ToRoleResponse(Role role)
    {
        return new RoleOptionResponse
        {
            RoleId = role.RoleId,
            Name = role.Name,
            Description = role.Description
        };
    }
}