using Vpims.Application.DTOs.Customers;

namespace Vpims.Application.DTOs.Dashboard;

public sealed class DashboardSummaryResponse
{
    public DashboardInventorySummaryResponse? Inventory { get; set; }

    public DashboardStaffSummaryResponse? Staff { get; set; }

    public CustomerDetailResponse? CurrentCustomer { get; set; }
}