using Vpims.Domain.Entities;

namespace Vpims.Application.DTOs.Sales;

public sealed class SaleItemResponse
{
    public int PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal => UnitPrice * Quantity;
}
