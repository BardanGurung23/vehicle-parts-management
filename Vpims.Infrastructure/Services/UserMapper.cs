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
            FullName = user.Customer?.FullName ?? user.FullName,
            Email = user.Customer?.Email ?? user.Email,
            PhoneNumber = user.Customer?.PhoneNumber ?? user.PhoneNumber,
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
            FullName = customer.FullName,
            Email = customer.Email ?? user.Email,
            PhoneNumber = customer.PhoneNumber,
            Address = customer.Address,
            Vehicles = customer.Vehicles
                .Select(ToVehicleResponse)
                .ToList()
        };
    }

    public static CustomerDetailResponse ToCustomerDetailResponse(Customer customer)
    {
        return new CustomerDetailResponse
        {
            CustomerId = customer.CustomerId,
            UserId = customer.UserId,
            FullName = customer.FullName,
            PhoneNumber = customer.PhoneNumber,
            Email = customer.Email,
            Address = customer.Address,
            RegisteredAt = customer.RegisteredAt,
            Vehicles = customer.Vehicles
                .OrderBy(vehicle => vehicle.VehicleNumber)
                .Select(ToVehicleResponse)
                .ToList()
        };
    }

    public static CustomerSearchResultResponse ToCustomerSearchResultResponse(Customer customer)
    {
        var vehicles = customer.Vehicles
            .OrderBy(vehicle => vehicle.VehicleNumber)
            .Select(ToVehicleResponse)
            .ToList();

        return new CustomerSearchResultResponse
        {
            CustomerId = customer.CustomerId,
            UserId = customer.UserId,
            FullName = customer.FullName,
            PhoneNumber = customer.PhoneNumber,
            Email = customer.Email,
            VehicleCount = vehicles.Count,
            Vehicles = vehicles
        };
    }

    public static VehicleResponse ToVehicleResponse(Vehicle vehicle)
    {
        return new VehicleResponse
        {
            VehicleId = vehicle.VehicleId,
            VehicleNumber = vehicle.VehicleNumber,
            Model = vehicle.Model
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