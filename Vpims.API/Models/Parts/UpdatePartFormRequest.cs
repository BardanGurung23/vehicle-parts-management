using System.ComponentModel.DataAnnotations;

namespace Vpims.API.Models.Parts;

public sealed class UpdatePartFormRequest
{
    [Required]
    [StringLength(150, MinimumLength = 1)]
    public string PartName { get; set; } = string.Empty;

    public string? Description { get; set; }

    [StringLength(500)]
    public string? ImageUrl { get; set; }

    public IFormFile? ImageFile { get; set; }

    public bool RemoveImage { get; set; }

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