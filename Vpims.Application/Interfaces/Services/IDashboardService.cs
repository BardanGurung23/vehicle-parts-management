using Vpims.Application.DTOs.Auth;
using Vpims.Application.DTOs.Dashboard;

namespace Vpims.Application.Interfaces.Services;

public interface IDashboardService
{
    Task<DashboardSummaryResponse> GetSummaryAsync(
        UserProfileResponse currentUser,
        CancellationToken cancellationToken = default);
}