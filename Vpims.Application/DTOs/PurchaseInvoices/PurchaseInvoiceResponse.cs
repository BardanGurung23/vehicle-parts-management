namespace Vpims.Application.DTOs.PurchaseInvoices;

public sealed class PurchaseInvoiceItemResponse
{
    public int PurchaseInvoiceItemId { get; set; }

    public int PartId { get; set; }

    public string PartName { get; set; } = string.Empty;

    public string PartNumber { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitCost { get; set; }

    public decimal LineTotal { get; set; }
}

public sealed class PurchaseInvoiceResponse
{
    public int PurchaseInvoiceId { get; set; }

    public int VendorId { get; set; }

    public string VendorName { get; set; } = string.Empty;

    public int CreatedByUserId { get; set; }

    public string CreatedByName { get; set; } = string.Empty;

    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTimeOffset InvoiceDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = string.Empty;

    public IReadOnlyList<PurchaseInvoiceItemResponse> Items { get; set; } = Array.Empty<PurchaseInvoiceItemResponse>();
}