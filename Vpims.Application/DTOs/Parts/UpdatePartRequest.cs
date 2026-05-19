using System.ComponentModel.DataAnnotations;

namespace Vpims.Application.DTOs.Parts;

public sealed class UpdatePartRequest
{
    [Required]
    [StringLength(150, MinimumLength = 1)]
    public string PartName { get; set; } = string.Empty;

    public string? Description { get; set; }

    [StringLength(500)]
    public string? ImageUrl { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal CostPrice { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    [Range(0, int.MaxValue)]
    public int ReorderLevel { get; set; }

    public int? PartCategoryId { get; set; }
}
