using System.ComponentModel.DataAnnotations;

namespace Vpims.Domain.Models;

public class VehiclePart
{
    public Guid Id { get; set; }

    [MaxLength(50)]
    public string PartNumber { get; set; } = string.Empty;

    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public int StockQuantity { get; set; }

    public decimal UnitPrice { get; set; }

    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}
