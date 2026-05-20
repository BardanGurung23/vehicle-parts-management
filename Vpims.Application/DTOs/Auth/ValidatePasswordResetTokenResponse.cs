namespace Vpims.Application.DTOs.Auth;

public sealed class ValidatePasswordResetTokenResponse
{
    public DateTimeOffset ExpiresAt { get; set; }
}