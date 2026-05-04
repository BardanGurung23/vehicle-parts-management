namespace Vpims.Domain.Entities;

public sealed class SalesInvoice
{
    public int SalesInvoiceId { get; set; }

    public int CustomerId { get; set; }

    public int? VehicleId { get; set; }

    public int CreatedByUserId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTimeOffset InvoiceDate { get; set; }

    public decimal Subtotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string PaymentStatus { get; set; } = "Pending";

    public DateTimeOffset? DueDate { get; set; }

    public Customer? Customer { get; set; }
}
