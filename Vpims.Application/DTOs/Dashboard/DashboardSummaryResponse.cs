using Vpims.Application.DTOs.Customers;
using Vpims.Application.DTOs.Alerts;

namespace Vpims.Application.DTOs.Dashboard;

public sealed class DashboardSummaryResponse
{
    public DashboardInventorySummaryResponse? Inventory { get; set; }

    public DashboardStaffSummaryResponse? Staff { get; set; }

    public AlertSummaryResponse? Alerts { get; set; }

    public CustomerDetailResponse? CurrentCustomer { get; set; }
}