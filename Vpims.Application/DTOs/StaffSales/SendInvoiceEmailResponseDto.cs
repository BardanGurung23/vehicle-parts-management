namespace Vpims.Application.DTOs.StaffSales;

public class SendInvoiceEmailResponseDto
{
    public Guid SaleId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public string RecipientEmail { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
