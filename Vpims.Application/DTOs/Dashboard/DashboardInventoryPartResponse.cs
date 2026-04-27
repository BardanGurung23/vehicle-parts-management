namespace Vpims.Application.DTOs.Dashboard;

public sealed class DashboardInventoryPartResponse
{
    public int PartId { get; set; }

    public string PartNumber { get; set; } = string.Empty;

    public string PartName { get; set; } = string.Empty;

    public int StockQuantity { get; set; }

    public int ReorderLevel { get; set; }

    public string? CategoryName { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}