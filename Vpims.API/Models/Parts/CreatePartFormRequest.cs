using System.ComponentModel.DataAnnotations;

namespace Vpims.API.Models.Parts;

public sealed class CreatePartFormRequest
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string PartNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(150, MinimumLength = 1)]
    public string PartName { get; set; } = string.Empty;

    public string? Description { get; set; }

    [StringLength(500)]
    public string? ImageUrl { get; set; }

    public IFormFile? ImageFile { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal CostPrice { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    [Range(0, int.MaxValue)]
    public int ReorderLevel { get; set; } = 10;

    public int? PartCategoryId { get; set; }
}