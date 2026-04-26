using Microsoft.AspNetCore.Identity;
using Vpims.Application.Common.Exceptions;
using Vpims.Application.DTOs.Customers;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Application.Interfaces.Services;
using Vpims.Domain.Entities;

namespace Vpims.Infrastructure.Services;

public sealed class CustomerService(
    ICustomerRepository customerRepository,
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    PasswordHasher<User> passwordHasher) : ICustomerService
{
    public async Task<RegisterCustomerResponse> RegisterAsync(RegisterCustomerRequest request, CancellationToken cancellationToken = default)
    {
        string email = InputNormalizer.NormalizeEmail(request.Email);
        string phoneNumber = InputNormalizer.NormalizePhoneNumber(request.PhoneNumber);
        string fullName = InputNormalizer.NormalizeFullName(request.FullName);
        string? vehicleNumber = NormalizeOptionalVehicleNumber(request.VehicleNumber);
        string? vehicleModel = NormalizeOptionalValue(request.VehicleModel);

        EnsureVehicleFields(vehicleNumber, vehicleModel);

        await EnsureUniqueCustomerIdentityAsync(email, phoneNumber, vehicleNumber, cancellationToken);

        Role customerRole = await roleRepository.GetByNameAsync(SystemRoles.Customer, cancellationToken)
            ?? throw new NotFoundException("The Customer role is missing from the database.");

        var user = new User
        {
            RoleId = customerRole.RoleId,
            FullName = fullName,
            Email = email,
            PhoneNumber = phoneNumber,
            CreatedAt = DateTimeOffset.UtcNow,
            IsActive = true
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        var customer = new Customer
        {
            FullName = fullName,
            PhoneNumber = phoneNumber,
            Email = email,
            Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            RegisteredAt = DateTimeOffset.UtcNow
        };

        Vehicle? vehicle = CreateVehicle(vehicleNumber, vehicleModel);

        Customer createdCustomer = await customerRepository.RegisterCustomerAsync(user, customer, vehicle, cancellationToken);
        return UserMapper.ToRegistrationResponse(user, createdCustomer);
    }

    public async Task<CustomerDetailResponse> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        string fullName = InputNormalizer.NormalizeFullName(request.FullName);
        string phoneNumber = InputNormalizer.NormalizePhoneNumber(request.PhoneNumber);
        string? email = NormalizeOptionalEmail(request.Email);
        string vehicleNumber = InputNormalizer.NormalizeVehicleNumber(request.VehicleNumber);
        string? vehicleModel = NormalizeOptionalValue(request.VehicleModel);

        await EnsureUniqueCustomerIdentityAsync(email, phoneNumber, vehicleNumber, cancellationToken);

        var customer = new Customer
        {
            FullName = fullName,
            PhoneNumber = phoneNumber,
            Email = email,
            Address = NormalizeOptionalValue(request.Address),
            RegisteredAt = DateTimeOffset.UtcNow
        };

        var vehicle = new Vehicle
        {
            VehicleNumber = vehicleNumber,
            Model = vehicleModel,
            CreatedAt = DateTimeOffset.UtcNow
        };

        Customer createdCustomer = await customerRepository.CreateStaffCustomerAsync(customer, vehicle, cancellationToken);
        return UserMapper.ToCustomerDetailResponse(createdCustomer);
    }

    public async Task<IReadOnlyList<CustomerSearchResultResponse>> SearchCustomersAsync(
        SearchCustomersRequest request,
        CancellationToken cancellationToken = default)
    {
        int? customerId = request.CustomerId;
        string? phoneNumber = NormalizeOptionalPhoneNumber(request.PhoneNumber);
        string? vehicleNumber = NormalizeOptionalVehicleNumber(request.VehicleNumber);
        string? name = NormalizeOptionalName(request.Name);

        if (!customerId.HasValue
            && phoneNumber is null
            && vehicleNumber is null
            && name is null)
        {
            throw new AppValidationException("At least one search filter is required.");
        }

        IReadOnlyList<Customer> customers = await customerRepository.SearchAsync(
            customerId,
            phoneNumber,
            vehicleNumber,
            name,
            cancellationToken);

        return customers
            .Select(UserMapper.ToCustomerSearchResultResponse)
            .ToList();
    }

    public async Task<CustomerDetailResponse> GetCustomerByIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        Customer customer = await customerRepository.GetByIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException($"Customer with id {customerId} not found.");

        return UserMapper.ToCustomerDetailResponse(customer);
    }

    public async Task<CustomerDetailResponse> GetCustomerByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        Customer customer = await customerRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Customer profile not found.");

        return UserMapper.ToCustomerDetailResponse(customer);
    }

    private async Task EnsureUniqueCustomerIdentityAsync(
        string? email,
        string phoneNumber,
        string? vehicleNumber,
        CancellationToken cancellationToken)
    {
        if (await customerRepository.ExistsByPhoneNumberAsync(phoneNumber, cancellationToken)
            || await userRepository.ExistsByPhoneNumberAsync(phoneNumber, cancellationToken))
        {
            throw new AppValidationException("A customer with this phone number already exists.");
        }

        if (!string.IsNullOrWhiteSpace(email)
            && (await customerRepository.ExistsByEmailAsync(email, cancellationToken)
                || await userRepository.ExistsByEmailAsync(email, cancellationToken)))
        {
            throw new AppValidationException("A user with this email already exists.");
        }

        if (!string.IsNullOrWhiteSpace(vehicleNumber)
            && await customerRepository.ExistsByVehicleNumberAsync(vehicleNumber, cancellationToken))
        {
            throw new AppValidationException("A vehicle with this number already exists.");
        }
    }

    private static Vehicle? CreateVehicle(string? vehicleNumber, string? vehicleModel)
    {
        if (vehicleNumber is null)
        {
            return null;
        }

        return new Vehicle
        {
            VehicleNumber = vehicleNumber,
            Model = vehicleModel,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static void EnsureVehicleFields(string? vehicleNumber, string? vehicleModel)
    {
        if (vehicleNumber is null && vehicleModel is not null)
        {
            throw new AppValidationException("Vehicle number is required when providing vehicle details.");
        }
    }

    private static string? NormalizeOptionalEmail(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : InputNormalizer.NormalizeEmail(value);
    }

    private static string? NormalizeOptionalName(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : InputNormalizer.NormalizeFullName(value);
    }

    private static string? NormalizeOptionalPhoneNumber(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : InputNormalizer.NormalizePhoneNumber(value);
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string? NormalizeOptionalVehicleNumber(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : InputNormalizer.NormalizeVehicleNumber(value);
    }
}