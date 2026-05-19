namespace Vpims.Application.DTOs.Parts;

public sealed class PartResponse
{
    public int PartId { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal CostPrice { get; set; }
    public int StockQuantity { get; set; }
    public int ReorderLevel { get; set; }
    public int? PartCategoryId { get; set; }
    public string? CategoryName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
