using System.ComponentModel.DataAnnotations;
using Vpims.Domain.Entities;

namespace Vpims.Domain.Entities;

public sealed class Sale
{
    public int SaleId { get; set; }

    public int CustomerId { get; set; }

    public int? VehicleId { get; set; }

    public int CreatedByUserId { get; set; }

    [Required]
    [MaxLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Subtotal { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DiscountAmount { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    [Required]
    [MaxLength(30)]
    public string PaymentStatus { get; set; } = "Paid";

    public DateTimeOffset? DueDate { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset SaleDate { get; set; }

    public User? CreatedByUser { get; set; }

    public Customer? Customer { get; set; }

    public Vehicle? Vehicle { get; set; }

    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
}
