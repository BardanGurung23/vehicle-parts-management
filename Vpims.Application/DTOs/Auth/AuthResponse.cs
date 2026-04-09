namespace Vpims.Application.DTOs.Auth;

public sealed class AuthResponse
{
    public string Token { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public UserProfileResponse User { get; set; } = new();
}