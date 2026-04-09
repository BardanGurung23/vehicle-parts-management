using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Vpims.Application.Common.Exceptions;
using Vpims.Application.DTOs.Auth;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Application.Interfaces.Services;
using Vpims.Domain.Entities;
using Vpims.Infrastructure.Security;

namespace Vpims.Infrastructure.Services;

public sealed class AuthService(
    IUserRepository userRepository,
    PasswordHasher<User> passwordHasher,
    JwtTokenGenerator jwtTokenGenerator) : IAuthService
{
    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        string email = InputNormalizer.NormalizeEmail(request.Email);

        User user = await userRepository.GetByEmailAsync(email, cancellationToken)
            ?? throw new AppValidationException("Invalid email or password.");

        if (!user.IsActive)
        {
            throw new AppValidationException("This account is inactive.");
        }

        PasswordVerificationResult verificationResult;

        try
        {
            verificationResult = passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password);
        }
        catch (FormatException)
        {
            throw new AppValidationException("Invalid email or password.");
        }

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new AppValidationException("Invalid email or password.");
        }

        var (token, expiresAt) = jwtTokenGenerator.Generate(user);

        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = UserMapper.ToProfile(user)
        };
    }

    public async Task<UserProfileResponse> GetCurrentUserAsync(ClaimsPrincipal claimsPrincipal, CancellationToken cancellationToken = default)
    {
        string? userIdValue = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? claimsPrincipal.FindFirstValue(ClaimTypes.Name)
            ?? claimsPrincipal.FindFirstValue("sub");

        if (!int.TryParse(userIdValue, out int userId))
        {
            throw new UnauthorizedAccessException("Invalid access token.");
        }

        User user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("User not found.");

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("This account is inactive.");
        }

        return UserMapper.ToProfile(user);
    }
}