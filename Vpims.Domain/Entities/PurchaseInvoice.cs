using System.ComponentModel.DataAnnotations;

namespace Vpims.Domain.Entities;

public sealed class PurchaseInvoice
{
    public int PurchaseInvoiceId { get; set; }

    public int VendorId { get; set; }

    public int CreatedByUserId { get; set; }

    [Required]
    [MaxLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTimeOffset InvoiceDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Completed";

    public Vendor? Vendor { get; set; }

    public User? CreatedByUser { get; set; }

    public ICollection<PurchaseInvoiceItem> Items { get; set; } = new List<PurchaseInvoiceItem>();
}