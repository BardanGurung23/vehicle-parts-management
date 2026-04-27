namespace Vpims.Application.DTOs.Dashboard;

public sealed class DashboardInventorySummaryResponse
{
    public int TrackedPartCount { get; set; }

    public int LowStockCount { get; set; }

    public int OutOfStockCount { get; set; }

    public int TotalUnitsOnHand { get; set; }

    public decimal InventoryCost { get; set; }

    public IReadOnlyList<DashboardCountItemResponse> StockStatus { get; set; } = [];

    public IReadOnlyList<DashboardCountItemResponse> TopCategories { get; set; } = [];

    public IReadOnlyList<DashboardInventoryPartResponse> LowStockParts { get; set; } = [];

    public IReadOnlyList<DashboardInventoryPartResponse> RecentParts { get; set; } = [];
}