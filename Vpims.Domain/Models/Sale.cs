namespace Vpims.Domain.Models;

public class Sale
{
    public Guid Id { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public decimal Subtotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal FinalTotal { get; set; }

    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
}
