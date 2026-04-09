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

        await EnsureUniqueUserIdentityAsync(email, phoneNumber, cancellationToken);

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
            Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            RegisteredAt = DateTimeOffset.UtcNow
        };

        Customer createdCustomer = await customerRepository.RegisterCustomerAsync(user, customer, cancellationToken);
        user.Customer = createdCustomer;

        return UserMapper.ToRegistrationResponse(user, createdCustomer);
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