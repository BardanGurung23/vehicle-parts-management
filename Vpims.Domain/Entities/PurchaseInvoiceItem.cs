using System.ComponentModel.DataAnnotations;

namespace Vpims.Domain.Entities;

public sealed class PurchaseInvoiceItem
{
    public int PurchaseInvoiceItemId { get; set; }

    public int PurchaseInvoiceId { get; set; }

    public int PartId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitCost { get; set; }

    [Range(0, double.MaxValue)]
    public decimal LineTotal { get; set; }

    public PurchaseInvoice? PurchaseInvoice { get; set; }

    public Part? Part { get; set; }
}