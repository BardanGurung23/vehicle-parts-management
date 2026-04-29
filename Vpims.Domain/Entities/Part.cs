namespace Vpims.Domain.Entities;

public sealed class Part
{
    public int PartId { get; set; }

    public int? PartCategoryId { get; set; }

    public int? VendorId { get; set; }

    public string PartNumber { get; set; } = string.Empty;

    public string PartName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal CostPrice { get; set; }

    public int StockQuantity { get; set; }

    public int ReorderLevel { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public PartCategory? Category { get; set; }

    public Vendor? Vendor { get; set; }
}
