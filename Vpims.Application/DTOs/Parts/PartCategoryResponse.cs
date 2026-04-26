namespace Vpims.Application.DTOs.Parts;

public sealed class PartCategoryResponse
{
    public int PartCategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
