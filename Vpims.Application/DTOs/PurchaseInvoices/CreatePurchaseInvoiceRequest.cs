using System.ComponentModel.DataAnnotations;

namespace Vpims.Application.DTOs.PurchaseInvoices;

public sealed class CreatePurchaseInvoiceItemRequest
{
    [Required]
    public int PartId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitCost { get; set; }
}

public sealed class CreatePurchaseInvoiceRequest
{
    [Required]
    public int VendorId { get; set; }

    [MinLength(1)]
    public List<CreatePurchaseInvoiceItemRequest> Items { get; set; } = [];

    [MaxLength(30)]
    public string? Status { get; set; }
}