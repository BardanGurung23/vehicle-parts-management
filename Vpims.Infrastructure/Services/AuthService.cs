using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Vpims.Application.Common.Exceptions;
using Vpims.Application.DTOs.Auth;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Application.Interfaces;
using Vpims.Application.Interfaces.Services;
using Vpims.Domain.Entities;
using Vpims.Infrastructure.Options;
using Vpims.Infrastructure.Security;

namespace Vpims.Infrastructure.Services;

public sealed class AuthService(
    IUserRepository userRepository,
    IPasswordResetTokenRepository passwordResetTokenRepository,
    PasswordHasher<User> passwordHasher,
    JwtTokenGenerator jwtTokenGenerator,
    IEmailService emailService,
    IOptions<PasswordResetOptions> passwordResetOptions) : IAuthService
{
    private const string GenericForgotPasswordMessage = "If an active account matches that email, a password reset link has been sent.";
    private const string InvalidResetTokenMessage = "This password reset link is invalid or has expired.";
    private const string ResetSuccessMessage = "Your password has been reset successfully.";

    private readonly PasswordResetOptions resetOptions = passwordResetOptions.Value;

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

    public async Task<ForgotPasswordResponse> RequestPasswordResetAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        string email = InputNormalizer.NormalizeEmail(request.Email);
        User? user = await userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return new ForgotPasswordResponse { Message = GenericForgotPasswordMessage };
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        string rawToken = GenerateResetToken();

        await passwordResetTokenRepository.InvalidateActiveTokensForUserAsync(user.UserId, now, cancellationToken);

        await passwordResetTokenRepository.CreateAsync(
            new PasswordResetToken
            {
                UserId = user.UserId,
                TokenHash = ComputeTokenHash(rawToken),
                ExpiresAt = now.AddMinutes(GetTokenLifetimeMinutes()),
                CreatedAt = now
            },
            cancellationToken);

        await emailService.SendEmailAsync(
            user.Email,
            "Reset your Autonix password",
            BuildPasswordResetEmailBody(user, rawToken),
            cancellationToken);

        return new ForgotPasswordResponse
        {
            Message = GenericForgotPasswordMessage
        };
    }

    public async Task<ValidatePasswordResetTokenResponse> ValidatePasswordResetTokenAsync(
        ValidatePasswordResetTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        PasswordResetToken token = await GetValidPasswordResetTokenAsync(request.Token, cancellationToken);

        return new ValidatePasswordResetTokenResponse
        {
            ExpiresAt = token.ExpiresAt
        };
    }

    public async Task<ResetPasswordResponse> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            throw new AppValidationException("Passwords do not match.");
        }

        PasswordResetToken passwordResetToken = await GetValidPasswordResetTokenAsync(request.Token, cancellationToken);
        User user = passwordResetToken.User ?? throw new AppValidationException(InvalidResetTokenMessage);

        if (!user.IsActive)
        {
            throw new AppValidationException(InvalidResetTokenMessage);
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        passwordResetToken.UsedAt = now;

        await passwordResetTokenRepository.InvalidateOtherActiveTokensForUserAsync(
            user.UserId,
            passwordResetToken.PasswordResetTokenId,
            now,
            cancellationToken);

        await userRepository.UpdateAsync(user, cancellationToken);

        return new ResetPasswordResponse
        {
            Message = ResetSuccessMessage
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

    private async Task<PasswordResetToken> GetValidPasswordResetTokenAsync(string token, CancellationToken cancellationToken)
    {
        string normalizedToken = NormalizeResetToken(token);
        string tokenHash = ComputeTokenHash(normalizedToken);
        PasswordResetToken? passwordResetToken = await passwordResetTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (passwordResetToken is null ||
            passwordResetToken.UsedAt is not null ||
            passwordResetToken.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new AppValidationException(InvalidResetTokenMessage);
        }

        return passwordResetToken;
    }

    private string BuildPasswordResetEmailBody(User user, string rawToken)
    {
        string resetLink = BuildPasswordResetLink(rawToken);
        string encodedName = WebUtility.HtmlEncode(user.FullName);
        string encodedLink = WebUtility.HtmlEncode(resetLink);
        int tokenLifetimeMinutes = GetTokenLifetimeMinutes();

        return $"""
            <div style=\"font-family:Segoe UI,Arial,sans-serif;color:#1f2933;line-height:1.6;max-width:640px;\">
                <h2 style=\"margin-bottom:12px;\">Reset your Autonix password</h2>
                <p>Hello {encodedName},</p>
                <p>We received a request to reset your password. Use the button below to choose a new one. This link expires in {tokenLifetimeMinutes} minutes.</p>
                <p style=\"margin:24px 0;\">
                    <a href=\"{encodedLink}\" style=\"display:inline-block;background:#0f172a;color:#ffffff;text-decoration:none;padding:12px 20px;border-radius:10px;font-weight:600;\">Reset password</a>
                </p>
                <p>If the button does not work, copy and paste this URL into your browser:</p>
                <p><a href=\"{encodedLink}\">{encodedLink}</a></p>
                <p>If you did not request this change, you can ignore this email.</p>
            </div>
            """;
    }

    private string BuildPasswordResetLink(string token)
    {
        if (string.IsNullOrWhiteSpace(resetOptions.FrontendBaseUrl))
        {
            throw new AppValidationException("Password reset settings are incomplete. Update the PasswordReset section in appsettings before sending reset emails.");
        }

        string normalizedBaseUrl = resetOptions.FrontendBaseUrl.Trim().TrimEnd('/') + "/";

        if (!Uri.TryCreate(normalizedBaseUrl, UriKind.Absolute, out Uri? baseUri))
        {
            throw new AppValidationException("Password reset settings are incomplete. Update the PasswordReset section in appsettings before sending reset emails.");
        }

        string encodedToken = Uri.EscapeDataString(token);
        return new Uri(baseUri, $"reset-password?token={encodedToken}").ToString();
    }

    private int GetTokenLifetimeMinutes()
    {
        if (resetOptions.TokenLifetimeMinutes <= 0)
        {
            throw new AppValidationException("Password reset settings are incomplete. Update the PasswordReset section in appsettings before sending reset emails.");
        }

        return resetOptions.TokenLifetimeMinutes;
    }

    private static string GenerateResetToken()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string NormalizeResetToken(string token)
    {
        string normalizedToken = token.Trim();

        if (string.IsNullOrWhiteSpace(normalizedToken))
        {
            throw new AppValidationException(InvalidResetTokenMessage);
        }

        return normalizedToken;
    }

    private static string ComputeTokenHash(string token)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}