using Vpims.Application.DTOs.Users;

namespace Vpims.Application.DTOs.Dashboard;

public sealed class DashboardStaffSummaryResponse
{
    public int TotalStaffCount { get; set; }

    public int ActiveStaffCount { get; set; }

    public IReadOnlyList<DashboardCountItemResponse> RoleBreakdown { get; set; } = [];

    public IReadOnlyList<StaffUserResponse> RecentStaff { get; set; } = [];
}