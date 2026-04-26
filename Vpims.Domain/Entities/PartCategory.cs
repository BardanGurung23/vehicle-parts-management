namespace Vpims.Domain.Entities;

public sealed class PartCategory
{
    public int PartCategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<Part> Parts { get; set; } = new List<Part>();
}
