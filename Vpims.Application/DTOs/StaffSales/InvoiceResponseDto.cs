namespace Vpims.Application.DTOs.StaffSales;

public class InvoiceResponseDto
{
    public Guid SaleId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public Guid CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    public decimal Subtotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal FinalTotal { get; set; }

    public IReadOnlyCollection<InvoiceItemResponseDto> Items { get; set; } = [];
}
