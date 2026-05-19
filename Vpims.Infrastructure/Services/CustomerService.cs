using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using Vpims.Application.Common.Exceptions;
using Vpims.Application.DTOs.Auth;
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
    private static readonly EmailAddressAttribute EmailAddressValidator = new();

    public async Task<RegisterCustomerResponse> RegisterAsync(RegisterCustomerRequest request, CancellationToken cancellationToken = default)
    {
        string email = NormalizeRequiredEmail(request.Email);
        string phoneNumber = NormalizeRequiredPhoneNumber(request.PhoneNumber);
        string fullName = NormalizeRequiredFullName(request.FullName);
        string? vehicleNumber = NormalizeOptionalVehicleNumber(request.VehicleNumber);
        string? vehicleModel = NormalizeOptionalModel(request.VehicleModel);
        string? address = NormalizeOptionalValue(request.Address);

        EnsureVehicleFields(vehicleNumber, vehicleModel);

        Customer? existingCustomer = await FindClaimCandidateAsync(phoneNumber, email, vehicleNumber, cancellationToken);

        if (existingCustomer is not null)
        {
            return await AttachPortalUserToExistingCustomerAsync(
                existingCustomer,
                fullName,
                email,
                phoneNumber,
                address,
                request.Password,
                vehicleNumber,
                vehicleModel,
                cancellationToken);
        }

        await EnsureUniqueCustomerIdentityAsync(email, phoneNumber, vehicleNumber, cancellationToken);

        User user = await CreateCustomerPortalUserAsync(fullName, email, phoneNumber, request.Password, cancellationToken);

        var customer = new Customer
        {
            FullName = fullName,
            PhoneNumber = phoneNumber,
            Email = email,
            Address = address,
            RegisteredAt = DateTimeOffset.UtcNow
        };

        Vehicle? vehicle = CreateVehicle(vehicleNumber, vehicleModel);

        Customer createdCustomer = await customerRepository.RegisterCustomerAsync(user, customer, vehicle, cancellationToken);
        return UserMapper.ToRegistrationResponse(user, createdCustomer);
    }

    public async Task<CustomerDetailResponse> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        string fullName = NormalizeRequiredFullName(request.FullName);
        string phoneNumber = NormalizeRequiredPhoneNumber(request.PhoneNumber);
        string? email = NormalizeOptionalEmail(request.Email);
        string vehicleNumber = NormalizeRequiredVehicleNumber(request.VehicleNumber);
        string? vehicleModel = NormalizeOptionalModel(request.VehicleModel);

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

    public async Task<IReadOnlyList<CustomerSearchResultResponse>> GetCustomersAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Customer> customers = await customerRepository.GetAllAsync(cancellationToken);

        return customers
            .Select(UserMapper.ToCustomerSearchResultResponse)
            .ToList();
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

    public async Task<CustomerDetailResponse> UpdateCustomerProfileAsync(
        UserProfileResponse currentUser,
        UpdateCustomerProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        Customer customer = await customerRepository.GetTrackedByUserIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("Customer profile not found.");

        string fullName = NormalizeRequiredFullName(request.FullName);
        string phoneNumber = NormalizeRequiredPhoneNumber(request.PhoneNumber);
        string email = NormalizeOptionalEmail(request.Email) ?? customer.Email ?? customer.User?.Email
            ?? throw new AppValidationException("Customer email is required.");
        string? address = NormalizeOptionalValue(request.Address);

        await EnsureUniqueProfileIdentityAsync(customer, email, phoneNumber, cancellationToken);

        customer.FullName = fullName;
        customer.PhoneNumber = phoneNumber;
        customer.Email = email;
        customer.Address = address;

        if (customer.User is not null)
        {
            customer.User.FullName = fullName;
            customer.User.PhoneNumber = phoneNumber;
            customer.User.Email = email;
        }

        Customer updatedCustomer = await customerRepository.UpdateAsync(customer, cancellationToken);
        return UserMapper.ToCustomerDetailResponse(updatedCustomer);
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

    private async Task<Customer?> FindClaimCandidateAsync(
        string phoneNumber,
        string email,
        string? vehicleNumber,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Customer> candidates = await customerRepository.GetUnlinkedRegistrationCandidatesAsync(
            phoneNumber,
            email,
            vehicleNumber,
            cancellationToken);

        if (candidates.Count == 0)
        {
            return null;
        }

        if (candidates.Count > 1)
        {
            throw new AppValidationException("We found conflicting customer records for this registration. Please contact staff for assistance.");
        }

        return candidates[0];
    }

    private async Task<RegisterCustomerResponse> AttachPortalUserToExistingCustomerAsync(
        Customer customer,
        string fullName,
        string email,
        string phoneNumber,
        string? address,
        string password,
        string? vehicleNumber,
        string? vehicleModel,
        CancellationToken cancellationToken)
    {
        await EnsureClaimIdentityAsync(customer, email, phoneNumber, vehicleNumber, cancellationToken);

        User user = await CreateCustomerPortalUserAsync(fullName, email, phoneNumber, password, cancellationToken);

        customer.FullName = fullName;
        customer.PhoneNumber = phoneNumber;
        customer.Email = email;
        customer.Address = address;

        Vehicle? vehicleToCreate = UpsertClaimVehicle(customer, vehicleNumber, vehicleModel);

        Customer claimedCustomer = await customerRepository.AttachPortalUserAsync(user, customer, vehicleToCreate, cancellationToken);
        return UserMapper.ToRegistrationResponse(user, claimedCustomer);
    }

    private async Task EnsureClaimIdentityAsync(
        Customer customer,
        string email,
        string phoneNumber,
        string? vehicleNumber,
        CancellationToken cancellationToken)
    {
        if (await userRepository.ExistsByPhoneNumberAsync(phoneNumber, cancellationToken))
        {
            throw new AppValidationException("A customer with this phone number already exists.");
        }

        if (await userRepository.ExistsByEmailAsync(email, cancellationToken))
        {
            throw new AppValidationException("A user with this email already exists.");
        }

        if (!string.Equals(customer.PhoneNumber, phoneNumber, StringComparison.Ordinal)
            && await customerRepository.ExistsByPhoneNumberAsync(phoneNumber, cancellationToken))
        {
            throw new AppValidationException("A customer with this phone number already exists.");
        }

        if (!string.Equals(customer.Email, email, StringComparison.Ordinal)
            && await customerRepository.ExistsByEmailAsync(email, cancellationToken))
        {
            throw new AppValidationException("A user with this email already exists.");
        }

        if (!string.IsNullOrWhiteSpace(vehicleNumber)
            && !customer.Vehicles.Any(vehicle => string.Equals(vehicle.VehicleNumber, vehicleNumber, StringComparison.Ordinal))
            && await customerRepository.ExistsByVehicleNumberAsync(vehicleNumber, cancellationToken))
        {
            throw new AppValidationException("A vehicle with this number already exists.");
        }
    }

    private async Task<User> CreateCustomerPortalUserAsync(
        string fullName,
        string email,
        string phoneNumber,
        string password,
        CancellationToken cancellationToken)
    {
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

        user.PasswordHash = passwordHasher.HashPassword(user, password);
        return user;
    }

    private static Vehicle? UpsertClaimVehicle(Customer customer, string? vehicleNumber, string? vehicleModel)
    {
        if (vehicleNumber is null)
        {
            return null;
        }

        Vehicle? existingVehicle = customer.Vehicles.FirstOrDefault(vehicle => vehicle.VehicleNumber == vehicleNumber);

        if (existingVehicle is not null)
        {
            if (vehicleModel is not null)
            {
                existingVehicle.Model = vehicleModel;
            }

            return null;
        }

        return new Vehicle
        {
            VehicleNumber = vehicleNumber,
            Model = vehicleModel,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private async Task EnsureUniqueProfileIdentityAsync(
        Customer customer,
        string email,
        string phoneNumber,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(customer.PhoneNumber, phoneNumber, StringComparison.Ordinal)
            && (await customerRepository.ExistsByPhoneNumberAsync(phoneNumber, cancellationToken)
                || await userRepository.ExistsByPhoneNumberAsync(phoneNumber, cancellationToken)))
        {
            throw new AppValidationException("A customer with this phone number already exists.");
        }

        string? currentEmail = customer.Email;
        bool emailChanged = !string.Equals(currentEmail, email, StringComparison.Ordinal);

        if (emailChanged
            && (await customerRepository.ExistsByEmailAsync(email, cancellationToken)
                || await userRepository.ExistsByEmailAsync(email, cancellationToken)))
        {
            throw new AppValidationException("A user with this email already exists.");
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
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string email = InputNormalizer.NormalizeEmail(value);

        if (email.Length > 150 || !EmailAddressValidator.IsValid(email))
        {
            throw new AppValidationException("Enter a valid email address.");
        }

        return email;
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
            : NormalizeRequiredPhoneNumber(value);
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string? NormalizeOptionalVehicleNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return NormalizeRequiredVehicleNumber(value);
    }

    private static string NormalizeRequiredFullName(string value)
    {
        string fullName = InputNormalizer.NormalizeFullName(value);

        if (fullName.Length < 3 || fullName.Length > 150)
        {
            throw new AppValidationException("Full name must be between 3 and 150 characters.");
        }

        return fullName;
    }

    private static string NormalizeRequiredEmail(string value)
    {
        string? email = NormalizeOptionalEmail(value);
        return email ?? throw new AppValidationException("Email is required.");
    }

    private static string NormalizeRequiredPhoneNumber(string value)
    {
        string phoneNumber = InputNormalizer.NormalizePhoneNumber(value);
        int digitCount = phoneNumber.Count(char.IsDigit);

        if (digitCount < 7 || phoneNumber.Length > 20)
        {
            throw new AppValidationException("Phone number must contain 7 to 20 digits, with an optional leading plus sign.");
        }

        return phoneNumber;
    }

    private static string NormalizeRequiredVehicleNumber(string value)
    {
        string vehicleNumber = InputNormalizer.NormalizeVehicleNumber(value);

        if (vehicleNumber.Length < 2 || vehicleNumber.Length > 30)
        {
            throw new AppValidationException("Vehicle number must be between 2 and 30 characters.");
        }

        return vehicleNumber;
    }

    private static string? NormalizeOptionalModel(string? value)
    {
        string? model = NormalizeOptionalValue(value);

        if (model?.Length > 80)
        {
            throw new AppValidationException("Vehicle model is too long.");
        }

        return model;
    }

    public async Task<VehicleResponse> AddVehicleAsync(
        UserProfileResponse currentUser,
        CreateVehicleRequest request,
        CancellationToken cancellationToken = default)
    {
        Customer customer = await customerRepository.GetByUserIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("Customer profile not found.");

        string vehicleNumber = NormalizeRequiredVehicleNumber(request.VehicleNumber);

        if (await customerRepository.ExistsByVehicleNumberAsync(vehicleNumber, cancellationToken))
        {
            throw new AppValidationException("A vehicle with this number already exists.");
        }

        var vehicle = new Vehicle
        {
            VehicleNumber = vehicleNumber,
            Model = NormalizeOptionalModel(request.Model),
            CreatedAt = DateTimeOffset.UtcNow
        };

        Vehicle created = await customerRepository.AddVehicleAsync(customer.CustomerId, vehicle, cancellationToken);
        return UserMapper.ToVehicleResponse(created);
    }

    public async Task<VehicleResponse> UpdateVehicleAsync(
        UserProfileResponse currentUser,
        int vehicleId,
        UpdateVehicleRequest request,
        CancellationToken cancellationToken = default)
    {
        Customer customer = await customerRepository.GetTrackedByUserIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("Customer profile not found.");

        Vehicle vehicle = customer.Vehicles.FirstOrDefault(item => item.VehicleId == vehicleId)
            ?? throw new NotFoundException($"Vehicle with id {vehicleId} not found.");

        string vehicleNumber = NormalizeRequiredVehicleNumber(request.VehicleNumber);

        if (!string.Equals(vehicle.VehicleNumber, vehicleNumber, StringComparison.Ordinal)
            && await customerRepository.ExistsByVehicleNumberAsync(vehicleNumber, cancellationToken))
        {
            throw new AppValidationException("A vehicle with this number already exists.");
        }

        vehicle.VehicleNumber = vehicleNumber;
        vehicle.Model = NormalizeOptionalModel(request.Model);

        Vehicle updatedVehicle = await customerRepository.UpdateVehicleAsync(vehicle, cancellationToken);
        return UserMapper.ToVehicleResponse(updatedVehicle);
    }

    public async Task<CustomerDetailResponse> RemoveVehicleAsync(
        UserProfileResponse currentUser,
        int vehicleId,
        CancellationToken cancellationToken = default)
    {
        Customer customer = await customerRepository.GetTrackedByUserIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("Customer profile not found.");

        Vehicle vehicle = customer.Vehicles.FirstOrDefault(item => item.VehicleId == vehicleId)
            ?? throw new NotFoundException($"Vehicle with id {vehicleId} not found.");

        Customer updatedCustomer = await customerRepository.RemoveVehicleAsync(customer, vehicle, cancellationToken);
        return UserMapper.ToCustomerDetailResponse(updatedCustomer);
    }

    public async Task<IReadOnlyList<VehicleResponse>> GetMyVehiclesAsync(
        UserProfileResponse currentUser,
        CancellationToken cancellationToken = default)
    {
        Customer customer = await customerRepository.GetByUserIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("Customer profile not found.");

        IReadOnlyList<Vehicle> vehicles = await customerRepository.GetVehiclesByCustomerIdAsync(
            customer.CustomerId,
            cancellationToken);

        return vehicles.Select(UserMapper.ToVehicleResponse).ToList();
    }
}