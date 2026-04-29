using System.ComponentModel.DataAnnotations;
using Vpims.Domain.Entities;

namespace Vpims.Domain.Entities;

public sealed class SaleItem
{
    public int SaleItemId { get; set; }

    public int SaleId { get; set; }

    public int PartId { get; set; }

    public int Quantity { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    public Sale? Sale { get; set; }

    public Part? Part { get; set; }
}
