namespace Vpims.Application.DTOs.Alerts;

public sealed class LowStockAlertResponse
{
    public int PartId { get; set; }

    public string PartNumber { get; set; } = string.Empty;

    public string PartName { get; set; } = string.Empty;

    public string? CategoryName { get; set; }

    public int StockQuantity { get; set; }

    public int Threshold { get; set; }
}