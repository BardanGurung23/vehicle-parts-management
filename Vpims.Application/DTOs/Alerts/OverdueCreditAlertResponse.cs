namespace Vpims.Application.DTOs.Alerts;

public sealed class OverdueCreditAlertResponse
{
    public int SaleId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string? CustomerEmail { get; set; }

    public decimal OutstandingAmount { get; set; }

    public string PaymentStatus { get; set; } = string.Empty;

    public DateTimeOffset? DueDate { get; set; }

    public int DaysOverdue { get; set; }
}