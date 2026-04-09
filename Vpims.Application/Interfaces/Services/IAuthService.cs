using System.Security.Claims;
using Vpims.Application.DTOs.Auth;

namespace Vpims.Application.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<UserProfileResponse> GetCurrentUserAsync(ClaimsPrincipal claimsPrincipal, CancellationToken cancellationToken = default);
}