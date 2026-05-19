namespace Vpims.Application.DTOs.Sales;

public sealed class SendSaleInvoiceEmailResponse
{
    public int SaleId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public string RecipientEmail { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}